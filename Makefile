.PHONY: setup
setup:
	docker-compose build

.PHONY: build
build:
	docker-compose build person-listener

.PHONY: serve
serve:
	docker-compose build person-listener && docker-compose up person-listener

.PHONY: shell
shell:
	docker-compose run person-listener bash

.PHONY: test
test:
	docker-compose up dynamodb-database & docker-compose build person-listener-test && docker-compose up person-listener-test

.PHONY: lint
lint:
	-dotnet tool install -g dotnet-format
	dotnet tool update -g dotnet-format
	dotnet format

.PHONY: restart-db
restart-db:
	docker stop $$(docker ps -q --filter ancestor=dynamodb-database -a)
	-docker rm $$(docker ps -q --filter ancestor=dynamodb-database -a)
	docker rmi dynamodb-database
	docker-compose up -d dynamodb-database
