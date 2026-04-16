import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getOrders } from '../services/api'

export default function FailedOrders() {
  const [orders, setOrders] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const navigate = useNavigate()

  useEffect(() => {
    loadOrders()
  }, [])

  async function loadOrders() {
    try {
      setLoading(true)
      const data = await getOrders()
      const failedOnly = data.filter(o => (o.status || '').toLowerCase() === 'failed')
      setOrders(failedOnly)
      setError('')
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  function getTotal(order) {
    return (order.items || []).reduce((sum, item) => sum + (item.price * item.quantity), 0)
  }

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Failed Orders</h2>
        <button className="btn btn-danger" onClick={loadOrders}>Refresh</button>
      </div>

      {loading && <div className="alert alert-info">Loading failed orders...</div>}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && (
        <div className="card shadow-sm border-0">
          <div className="card-body">
            <div className="table-responsive">
              <table className="table table-hover align-middle">
                <thead className="table-light">
                  <tr>
                    <th>Order ID</th>
                    <th>Customer</th>
                    <th>Total</th>
                    <th>Created</th>
                  </tr>
                </thead>
                <tbody>
                  {orders.map(order => (
                    <tr
                      key={order.id}
                      style={{ cursor: 'pointer' }}
                      onClick={() => navigate(`/orders/${order.id}`)}
                    >
                      <td>#{order.id}</td>
                      <td>{order.customerName}</td>
                      <td>€{getTotal(order).toFixed(2)}</td>
                      <td>{new Date(order.createdAtUtc).toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {orders.length === 0 && (
              <div className="alert alert-secondary mb-0">No failed orders found.</div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}