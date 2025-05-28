terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 3.0"
    }
  }
}

provider "aws" {
  region = "eu-west-2"
}

terraform {
  backend "s3" {
    bucket         = "housing-pre-production-terraform-state"
    encrypt        = true
    region         = "eu-west-2"
    key            = "services/person-listener/state"
    dynamodb_table = "housing-pre-production-terraform-state-lock"
  }
}

data "aws_vpc" "housing_pre_production_vpc" {
  tags = {
    Name = "housing-pre-prod-pre-prod"
  }
}

module "person_listener_sg" {
  source             = "../modules/security_groups/outbound_only_traffic"
  vpc_id             = data.aws_vpc.housing_pre_production_vpc.id
  user_resource_name = "person_listener"
  environment_name   = var.environment_name
}

data "aws_ssm_parameter" "tenure_sns_topic_arn" {
  name = "/sns-topic/pre-production/tenure/arn"
}

data "aws_ssm_parameter" "accounts_sns_topic_arn" {
  name = "/sns-topic/pre-production/accounts/arn"
}

resource "aws_sqs_queue" "person_dead_letter_queue" {
  name                              = "persondeadletterqueue.fifo"
  fifo_queue                        = true
  content_based_deduplication       = true
  kms_master_key_id                 = "alias/housing-pre-production-cmk"
  kms_data_key_reuse_period_seconds = 300
}

resource "aws_sqs_queue" "person_queue" {
  name                              = "personqueue.fifo"
  fifo_queue                        = true
  content_based_deduplication       = true
  kms_master_key_id                 = "alias/housing-pre-production-cmk"
  kms_data_key_reuse_period_seconds = 300
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.person_dead_letter_queue.arn,
    maxReceiveCount     = 3
  })
}

resource "aws_sqs_queue_policy" "person_queue_policy" {
  queue_url = aws_sqs_queue.person_queue.id
  policy    = <<POLICY
  {
      "Version": "2012-10-17",
      "Id": "sqspolicy",
      "Statement": [
          {
              "Sid": "First",
              "Effect": "Allow",
              "Principal": "*",
              "Action": "sqs:SendMessage",
              "Resource": "${aws_sqs_queue.person_queue.arn}",
              "Condition": {
              "ArnEquals": {
                  "aws:SourceArn": "${data.aws_ssm_parameter.tenure_sns_topic_arn.value}"
              }
              }
          },          
          {
              "Sid": "Second",
              "Effect": "Allow",
              "Principal": "*",
              "Action": "sqs:SendMessage",
              "Resource": "${aws_sqs_queue.person_queue.arn}",
              "Condition": {
              "ArnEquals": {
                  "aws:SourceArn": "${data.aws_ssm_parameter.accounts_sns_topic_arn.value}"
              }
              }
          }
      ]
  }
  POLICY
}

resource "aws_sns_topic_subscription" "person_queue_subscribe_to_tenure_sns" {
  topic_arn            = data.aws_ssm_parameter.tenure_sns_topic_arn.value
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.person_queue.arn
  raw_message_delivery = true
}

resource "aws_sns_topic_subscription" "person_queue_subscribe_to_accounts_sns" {
  topic_arn            = data.aws_ssm_parameter.accounts_sns_topic_arn.value
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.person_queue.arn
  raw_message_delivery = true
}

resource "aws_ssm_parameter" "person_sqs_queue_arn" {
  name  = "/sqs-queue/pre-production/person/arn"
  type  = "String"
  value = aws_sqs_queue.person_queue.arn
}
