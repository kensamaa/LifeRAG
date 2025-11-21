Write-Host "Starting PostgreSQL with Docker Compose..." -ForegroundColor Green
docker-compose up -d

Write-Host "`nWaiting for PostgreSQL to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host "`nStarting LifeRAG API..." -ForegroundColor Green
Set-Location LifeRAG.Api
dotnet run
