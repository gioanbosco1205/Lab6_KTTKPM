#!/bin/bash

echo "🚀 Starting Insurance System - Part 15 Test"
echo "=========================================="

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker first."
    exit 1
fi

# Clean up any existing containers
echo "🧹 Cleaning up existing containers..."
docker compose down -v

# Build and start all services
echo "🏗️  Building and starting all services..."
docker compose up --build -d

# Wait for services to be ready
echo "⏳ Waiting for services to start..."
sleep 30

# Check service health
echo "🔍 Checking service health..."

services=("rabbitmq:15672" "redis:6379" "postgres:5432" "authservice:5001" "policyservice:5002" "chatservice:5003" "paymentservice:5004" "pricingservice:5005" "apigateway:8080")

for service in "${services[@]}"; do
    IFS=':' read -r name port <<< "$service"
    if nc -z localhost $port 2>/dev/null; then
        echo "✅ $name is running on port $port"
    else
        echo "❌ $name is not responding on port $port"
    fi
done

echo ""
echo "🎯 System Status:"
echo "=================="
echo "🐰 RabbitMQ Management: http://localhost:15672 (guest/guest)"
echo "🔐 Auth Service: http://localhost:5001"
echo "📋 Policy Service: http://localhost:5002"
echo "💬 Chat Service: http://localhost:5003"
echo "💳 Payment Service: http://localhost:5004"
echo "💰 Pricing Service: http://localhost:5005"
echo "🌐 API Gateway: http://localhost:8080"
echo ""
echo "🖥️  Client App: Open client-app/index.html in your browser"
echo "📝 Test API: Use test-part15.http file"
echo ""
echo "📋 Test Sequence (Part 15):"
echo "1. ✅ RabbitMQ is running"
echo "2. 🔄 PolicyService will publish events"
echo "3. 📨 ChatService will receive events"
echo "4. 🖥️  Client App will show real-time notifications"
echo ""
echo "🎮 Ready to test! Open client-app/index.html and start testing."

# Show logs for debugging
echo ""
echo "📊 To view logs, use:"
echo "docker compose logs -f [service-name]"
echo ""
echo "🛑 To stop all services:"
echo "docker compose down"