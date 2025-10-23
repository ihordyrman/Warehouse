import axios from 'axios'

const API_BASE_URL = 'http://localhost:5080'

const apiClient = axios.create({
    baseURL: API_BASE_URL, timeout: 10000, headers: {
        'Content-Type': 'application/json',
    }
})

apiClient.interceptors.response.use(response => response, error => {
    console.error('API Error:', error.response?.data || error.message)
    return Promise.reject(error)
})

export const marketAPI = {
    getMarkets: () => apiClient.get('/market'),
    getMarketById: (id) => apiClient.get(`/market/${id}`),
    getMarketCredentials: (marketId) => apiClient.get(`/market/${marketId}/credentials`),
}

export const balanceAPI = {
    getBalance: (marketType) => apiClient.get(`/balance/${marketType}`),
    getAccountSummary: (marketType) => apiClient.get(`/balance/${marketType}/account/summary`),
    getCurrencyBalance: (marketType, currency) => apiClient.get(`/balance/${marketType}/${currency}`),
    getNonZeroBalances: (marketType) => apiClient.get(`/balance/${marketType}/non-zero`),
    getTotalUsdtValue: (marketType) => apiClient.get(`/balance/${marketType}/total-usdt`),
}

export const workerAPI = {
    getWorkers: () => apiClient.get('/worker'),
    getEnabledWorkers: () => apiClient.get('/worker/enabled'),
    getWorkerById: (id) => apiClient.get(`/worker/${id}`),
    toggleWorker: (id, enabled) => apiClient.patch(`/worker/${id}/toggle`, {enabled}),
}

export default {
    marketAPI, workerAPI, balanceAPI
}
