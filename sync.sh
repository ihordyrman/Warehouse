CURRENT_DATE=$(date '+%Y-%m-%d')
BASE_DIR="/home/ihor/okx-data"
LOG_FILE="$BASE_DIR/monitor.log"
CURRENCIES=("OKB" "BTC" "SOL" "ETH" "DOGE" "XRP" "BCH" "LTC")

mkdir -p "$BASE_DIR"

log_message() {
    echo "$(date '+%Y-%m-%d %H:%M:%S') - $1" >> "$LOG_FILE"
}

process_currency() {
    local currency=$1
    local pair="${currency}-USDT"
    local data_dir="$BASE_DIR/$CURRENT_DATE"
    local data_file="$data_dir/${currency}_candlesticks.csv"

    mkdir -p $data_dir

    if [ ! -f "$data_file" ]; then
        touch $data_file
        echo "timestamp,open,high,low,close,volume,volume_quote,confirmed" >> "$data_file"
        log_message "[$pair] Created new data file"
    fi

    local api_url="https://www.okx.com/api/v5/market/candles?instId=${pair}&bar=1m&limit=10"
    local response=$(curl -s -L --max-time 10 "$api_url")

    if [ $? -ne 0 ]; then
        log_message "[$pair] Failed to fetch data from API"
        return 1
    fi

    if ! echo "$response" | jq empty 2>/dev/null; then
        log_message "[$pair] Invalid JSON response"
        return 1
    fi

    local code=$(echo "$response" | jq -r '.code')
    if [ "$code" != "0" ]; then
        local msg=$(echo "$response" | jq -r '.msg')
        log_message "[$pair] API error - Code: $code, Message: $msg"
        return 1
    fi

    local existing_timestamps=$(cut -d',' -f1 "$data_file" | tail -n +2)
    local new_count=0

    echo "$response" | jq -r '.data[] | @tsv' | while IFS=$'\t' read -r timestamp open high low close volume volume_quote volume_quote2 confirmed; do
        if [ "$confirmed" != "1" ]; then
            continue
        fi

        if echo "$existing_timestamps" | grep -q "^$timestamp$"; then
            continue
        fi

        echo "$timestamp,$open,$high,$low,$close,$volume,$volume_quote,$confirmed" >> "$data_file"
        ((new_count++))
    done

    if [ $new_count -gt 0 ]; then
        log_message "[$pair] Added $new_count new candlesticks"
    fi
}

success_count=0
fail_count=0

for currency in "${CURRENCIES[@]}"; do
    if process_currency "$currency"; then
        ((success_count++))
    else
        ((fail_count++))
    fi
done

log_message "Completed: $success_count successful, $fail_count failed"
