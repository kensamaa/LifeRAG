Write-Host "Installing LifeRAG Python RAG Microservice..." -ForegroundColor Green

python -m venv venv
.\venv\Scripts\Activate.ps1

pip install --upgrade pip
pip install -r requirements.txt

Write-Host "`nStarting services with Docker Compose (from root)..." -ForegroundColor Green
Set-Location ..
docker-compose up -d
Set-Location liferag-python

Write-Host "`nInstallation complete!" -ForegroundColor Green
Write-Host "Make sure Ollama is running with: ollama serve" -ForegroundColor Yellow
Write-Host "Pull the model with: ollama pull llama3.1:8b" -ForegroundColor Yellow
Write-Host "Start the service with: uvicorn main:app --reload --port 8000" -ForegroundColor Yellow
