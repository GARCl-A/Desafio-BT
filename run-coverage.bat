@echo off
echo Executando testes com cobertura...

dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

echo Instalando ReportGenerator...
dotnet tool install -g dotnet-reportgenerator-globaltool

echo Gerando relatorio HTML...
reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:"coverage/report" -reporttypes:Html

echo Abrindo relatorio...
start coverage/report/index.html

echo Cobertura concluida!