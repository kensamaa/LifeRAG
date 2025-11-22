#!/bin/bash

echo "Installing LifeRAG Python RAG Microservice..."

python -m venv venv
source venv/bin/activate

pip install --upgrade pip
pip install -r requirements.txt

echo "Starting services with Docker Compose (from root)..."
cd ..
docker-compose up -d
cd liferag-python

echo "Installation complete!"
echo "Make sure Ollama is running with: ollama serve"
echo "Pull the model with: ollama pull llama3.1:8b"
echo "Start the service with: uvicorn main:app --reload --port 8000"
