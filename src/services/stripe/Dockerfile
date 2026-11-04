FROM python:3.12-slim

WORKDIR /app

COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

COPY config.py stripe.py ./

ENTRYPOINT ["python", "stripe.py"]
CMD ["--check"]

