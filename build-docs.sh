#!/bin/bash

# DocFX Documentation Build Script for Auth0 ASP.NET Core API

echo "🔨 Building the project..."
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

echo ""
echo "📚 Generating documentation with DocFX..."
sudo docfx docs-source/docfx.json

if [ $? -ne 0 ]; then
    echo "❌ DocFX generation failed!"
    exit 1
fi

echo ""
echo "✅ Documentation generated successfully!"
echo ""
echo "📖 To view the documentation, run:"
echo "   sudo docfx serve docs"
echo ""
echo "   Then open your browser to: http://localhost:8080"
