#Project Variables
PROJECT ?= SolarCoffee
ORG_NAME ?= SolarCoffee
REPO_NAME ?= SolarCoffee

.PHONY: migrations db

migrations: 
	cd ./SolarCoffee.Data && dotnet ef --startup-project ../SolarCoffee.Web/ migrations add $(mname) && cd ..

db:
	cd ./SolarCoffee.Data && dotnet ef --startup-project ../SolarCoffee.Web/ database update && cd ..