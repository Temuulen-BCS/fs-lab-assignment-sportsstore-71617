const API_BASE_URL = 'http://localhost:5000/api/orders'

export async function getOrders() {
  const response = await fetch(API_BASE_URL)
  if (!response.ok) {
    throw new Error('Failed to fetch orders.')
  }
  return await response.json()
}

export async function getOrderById(id) {
  const response = await fetch(`${API_BASE_URL}/${id}`)
  if (!response.ok) {
    throw new Error('Failed to fetch order details.')
  }
  return await response.json()
}