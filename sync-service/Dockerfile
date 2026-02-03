FROM python:3.11-slim

WORKDIR /app

# Install dependencies
RUN pip install --no-cache-dir requests

# Copy sync script
COPY vault_pra_sync.py .

# Make script executable
RUN chmod +x vault_pra_sync.py

# Run as non-root user
RUN useradd -m -u 1000 syncuser && chown -R syncuser:syncuser /app
USER syncuser

ENTRYPOINT ["python3", "vault_pra_sync.py"]
