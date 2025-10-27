<template>
    <div class="min-h-screen bg-gray-50">
        <!-- Header -->
        <header class="bg-white shadow-sm border-b border-gray-200">
            <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                <div class="flex justify-between items-center h-16">
                    <h1 class="text-xl font-bold text-gray-900">Warehouse System</h1>
                    <div class="flex items-center space-x-4">
                        <span class="text-sm text-gray-500">Status:</span>
                        <span :class="systemStatusClass">
              {{ systemStatus }}
            </span>
                    </div>
                </div>
            </div>
        </header>

        <!-- Main Content -->
        <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
            <!-- System Overview -->
            <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
                <div class="card">
                    <div class="flex items-center">
                        <div class="flex-shrink-0 p-3 bg-blue-100 rounded-lg">
                            <svg class="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                      d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
                            </svg>
                        </div>
                        <div class="ml-4">
                            <p class="text-sm text-gray-500">Active Accounts</p>
                            <p class="text-2xl font-semibold text-gray-900">{{ activeAccountsCount }}</p>
                        </div>
                    </div>
                </div>

                <div class="card">
                    <div class="flex items-center">
                        <div class="flex-shrink-0 p-3 bg-green-100 rounded-lg">
                            <svg class="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 12h14M12 5l7 7-7 7"/>
                            </svg>
                        </div>
                        <div class="ml-4">
                            <p class="text-sm text-gray-500">Running Workers</p>
                            <p class="text-2xl font-semibold text-gray-900">{{ runningWorkersCount }}</p>
                        </div>
                    </div>
                </div>

                <div class="card">
                    <div class="flex items-center">
                        <div class="flex-shrink-0 p-3 bg-purple-100 rounded-lg">
                            <svg class="w-6 h-6 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                      d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
                            </svg>
                        </div>
                        <div class="ml-4">
                            <p class="text-sm text-gray-500">Total Balance</p>
                            <p class="text-2xl font-semibold text-gray-900">{{ formatTotalBalance }}</p>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Accounts Section -->
            <section class="mb-8">
                <div class="flex justify-between items-center mb-4">
                    <h2 class="text-lg font-semibold text-gray-900">Active Accounts</h2>
                    <button @click="refreshData" class="text-sm text-primary-600 hover:text-primary-700">
                        Refresh
                    </button>
                </div>

                <div v-if="loading.accounts" class="text-center py-12">
                    <div class="inline-flex items-center">
                        <svg class="animate-spin h-5 w-5 mr-3 text-primary-600" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" fill="none"/>
                            <path class="opacity-75" fill="currentColor"
                                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
                        </svg>
                        Loading accounts...
                    </div>
                </div>

                <div v-else-if="error.accounts" class="text-center py-12 text-red-600">
                    <p>{{ error.accounts }}</p>
                    <button @click="fetchAccounts" class="mt-2 text-sm underline">Retry</button>
                </div>

                <div v-else-if="accounts.length === 0" class="text-center py-12 text-gray-500">
                    <p>No accounts configured yet.</p>
                </div>

                <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    <AccountCard
                            v-for="account in accounts"
                            :key="account.id"
                            :market="account"
                            :has-credentials="account.hasCredentials"
                            :balance="account.balance"
                    />
                </div>
            </section>

            <!-- Workers Section -->
            <section>
                <div class="flex justify-between items-center mb-4">
                    <h2 class="text-lg font-semibold text-gray-900">Active Workers</h2>
                    <div class="flex items-center space-x-2">
                        <label class="text-sm text-gray-600">
                            <input
                                    type="checkbox"
                                    v-model="showOnlyEnabled"
                                    class="mr-1"
                            >
                            Show only running
                        </label>
                    </div>
                </div>

                <div v-if="loading.workers" class="text-center py-12">
                    <div class="inline-flex items-center">
                        <svg class="animate-spin h-5 w-5 mr-3 text-primary-600" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" fill="none"/>
                            <path class="opacity-75" fill="currentColor"
                                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
                        </svg>
                        Loading workers...
                    </div>
                </div>

                <div v-else-if="error.workers" class="text-center py-12 text-red-600">
                    <p>{{ error.workers }}</p>
                    <button @click="fetchWorkers" class="mt-2 text-sm underline">Retry</button>
                </div>

                <div v-else-if="filteredWorkers.length === 0" class="text-center py-12 text-gray-500">
                    <p>{{ showOnlyEnabled ? 'No running workers.' : 'No workers configured yet.' }}</p>
                </div>

                <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    <WorkerCard
                            v-for="worker in filteredWorkers"
                            :key="worker.id"
                            :worker="worker"
                            @toggle="handleWorkerToggle"
                    />
                </div>
            </section>
        </main>
    </div>
</template>

<script>
import {ref, computed, onMounted, onUnmounted} from 'vue'
import AccountCard from './components/AccountCard.vue'
import WorkerCard from './components/WorkerCard.vue'
import {marketAPI, workerAPI, balanceAPI} from './services/api.js'

export default {
    name: 'App',
    components: {
        AccountCard,
        WorkerCard
    },
    setup() {
        const markets = ref([])
        const workers = ref([])
        const accounts = ref([])
        const showOnlyEnabled = ref(false)
        const loading = ref({
            accounts: false,
            workers: false
        })
        const error = ref({
            accounts: null,
            workers: null
        })

        let refreshInterval = null

        const activeAccountsCount = computed(() =>
                accounts.value.length
        )

        const runningWorkersCount = computed(() =>
                workers.value.filter(w => w.enabled).length
        )

        const totalBalance = computed(() =>
                markets.value.reduce((sum, acc) => sum + (acc.balance?.total || 0), 0)
        )

        const formatTotalBalance = computed(() =>
                new Intl.NumberFormat('en-US', {
                    style: 'currency',
                    currency: 'USD'
                }).format(totalBalance.value)
        )

        const systemStatus = computed(() => {
            if (loading.value.accounts || loading.value.workers) return 'Loading...'
            if (error.value.accounts || error.value.workers) return 'Error'
            if (runningWorkersCount.value > 0) return 'Online'
            return 'Idle'
        })

        const systemStatusClass = computed(() => {
            if (systemStatus.value === 'Online') return 'badge badge-success'
            if (systemStatus.value === 'Error') return 'badge badge-danger'
            if (systemStatus.value === 'Loading...') return 'badge badge-warning'
            return 'badge'
        })

        const filteredWorkers = computed(() => {
            if (!showOnlyEnabled.value) return workers.value
            return workers.value.filter(w => w.enabled)
        })

        const fetchAccounts = async () => {
            loading.value.accounts = true
            error.value.accounts = null

            try {
                const response = await marketAPI.getMarkets()
                const accountsResponse = await marketAPI.getAccounts()
                markets.value = response.data || []
                accounts.value = accountsResponse.data || []

                for (const market of markets.value) {
                    try {
                        const balanceResponse = await balanceAPI.getTotalUsdtValue(market.type);
                        const totalUsdt = balanceResponse.data.totalUsdtValue || 0;

                        market.balance = {
                            available: totalUsdt,
                            inOrders: 0,
                            total: totalUsdt
                        }
                    } catch (err) {
                        console.error(`Error fetching balance for ${market.marketType}:`, err)
                        market.balance = {
                            available: 0,
                            inOrders: 0,
                            total: 0
                        }
                    }
                }
            } catch (err) {
                error.value.accounts = err.message || 'Failed to load accounts'
                console.error('Error fetching accounts:', err)
            } finally {
                loading.value.accounts = false
            }
        }

        const fetchWorkers = async () => {
            loading.value.workers = true
            error.value.workers = null

            try {
                const response = await workerAPI.getWorkers()
                workers.value = response.data || []
            } catch (err) {
                error.value.workers = err.message || 'Failed to load workers'
                console.error('Error fetching workers:', err)
            } finally {
                loading.value.workers = false
            }
        }

        const refreshData = async () => {
            await Promise.all([fetchAccounts(), fetchWorkers()])
        }

        onMounted(() => {
            refreshData()
            // Auto-refresh every 30 seconds
            refreshInterval = setInterval(refreshData, 30000)
        })

        onUnmounted(() => {
            if (refreshInterval) {
                clearInterval(refreshInterval)
            }
        })

        return {
            accounts: markets,
            workers,
            filteredWorkers,
            showOnlyEnabled,
            loading,
            error,
            activeAccountsCount,
            runningWorkersCount,
            formatTotalBalance,
            systemStatus,
            systemStatusClass,
            fetchAccounts,
            fetchWorkers,
            refreshData
        }
    }
}
</script>
