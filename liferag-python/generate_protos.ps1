python -m grpc_tools.protoc -I./protos --python_out=./protos --grpc_python_out=./protos ./protos/rag.proto
Write-Host "Proto files generated successfully" -ForegroundColor Green
