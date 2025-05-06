import logging
import requests
import azure.functions as func
import os

def main(req: func.HttpRequest) -> func.HttpResponse:
    symbol = req.params.get('symbol')
    if not symbol:
        return func.HttpResponse("Missing stock symbol in query string", status_code=400)

    api_key = os.environ.get('ALPHA_VANTAGE_API_KEY')
    if not api_key:
        return func.HttpResponse("API key not configured", status_code=500)

    url = f"https://www.alphavantage.co/query"
    params = {
        "function": "TIME_SERIES_INTRADAY",
        "symbol": symbol,
        "interval": "5min",
        "apikey": api_key
    }

    response = requests.get(url, params=params)
    if response.status_code != 200:
        return func.HttpResponse(f"Error fetching data: {response.text}", status_code=500)

    return func.HttpResponse(response.text, mimetype="application/json")
