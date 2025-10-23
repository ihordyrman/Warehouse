<template>
    <div class="card">
        <div class="flex justify-between items-start mb-4">
            <div>
                <h3 class="text-lg font-semibold text-gray-900">{{ market.name }}</h3>
                <span :class="marketTypeClass">{{ market.type }}</span>
            </div>
            <span :class="statusClass">
        {{ market.enabled ? 'Active' : 'Inactive' }}
      </span>
        </div>

        <div class="space-y-3">
            <div v-if="hasCredentials" class="text-sm text-gray-600">
                <svg class="w-4 h-4 inline mr-1 text-green-500" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd"
                          d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                          clip-rule="evenodd"/>
                </svg>
                API Credentials Configured
            </div>
            <div v-else class="text-sm text-gray-500">
                <svg class="w-4 h-4 inline mr-1 text-yellow-500" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd"
                          d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z"
                          clip-rule="evenodd"/>
                </svg>
                No API Credentials
            </div>

            <div class="pt-3 border-t border-gray-200">
                <div class="text-sm text-gray-600">
                    Balance Information
                </div>
                <div class="mt-2 space-y-1">
                    <div class="flex justify-between text-sm">
                        <span class="text-gray-500">Available:</span>
                        <span class="font-medium text-gray-900">{{ formatBalance(balance.available) }}</span>
                    </div>
                    <div class="flex justify-between text-sm">
                        <span class="text-gray-500">In Orders:</span>
                        <span class="font-medium text-gray-900">{{ formatBalance(balance.inOrders) }}</span>
                    </div>
                    <div class="flex justify-between text-sm font-semibold pt-2 border-t">
                        <span class="text-gray-700">Total:</span>
                        <span class="text-gray-900">{{ formatBalance(balance.total) }}</span>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script>
export default {
    name: 'AccountCard',
    props: {
        market: {
            type: Object,
            required: true,
            default: () => ({
                id: 0,
                name: '',
                type: '',
                enabled: false
            })
        },
        hasCredentials: {
            type: Boolean,
            default: false
        },
        balance: {
            type: Object,
            default: () => ({
                available: 0,
                inOrders: 0,
                total: 0
            })
        }
    },
    computed: {
        statusClass() {
            return this.market.enabled ? 'badge badge-success' : 'badge badge-warning'
        },
        marketTypeClass() {
            const typeClasses = {
                'OKX': 'badge badge-info',
                'Binance': 'badge badge-warning',
                'Coinbase': 'badge badge-success'
            }
            return typeClasses[this.market.type] || 'badge'
        }
    },
    methods: {
        formatBalance(value) {
            if (value === undefined || value === null) return '$0.00'
            return new Intl.NumberFormat('en-US', {
                style: 'currency',
                currency: 'USD'
            }).format(value)
        }
    }
}
</script>
