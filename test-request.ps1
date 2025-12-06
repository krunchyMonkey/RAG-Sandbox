# Test script for RAG Sandbox
# Start the service first with: dotnet run --urls http://localhost:5000

Write-Host "Testing RAG Sandbox API..." -ForegroundColor Cyan

# Test 1: Get available models
Write-Host "`n1. Getting available models..." -ForegroundColor Yellow
$modelsResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/chat/models" -Method GET
Write-Host "Available models:" -ForegroundColor Green
$modelsResponse | ConvertTo-Json -Depth 3

# Test 2: Chat with URL in message
Write-Host "`n2. Sending chat request with URL about Asmongold..." -ForegroundColor Yellow
$chatRequest = @{
    message = "Based on this article https://en.wikipedia.org/wiki/Asmongold, can you tell me about Asmongold"
} | ConvertTo-Json

$chatResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/chat" -Method POST -Body $chatRequest -ContentType "application/json"
Write-Host "Response:" -ForegroundColor Green
Write-Host "Session ID: $($chatResponse.sessionId)"
Write-Host "Web URL: $($chatResponse.webUrl)"
Write-Host "Message: $($chatResponse.message)"
