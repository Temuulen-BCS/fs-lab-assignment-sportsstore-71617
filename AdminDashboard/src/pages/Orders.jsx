import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getOrders } from '../services/api'

export default function Orders() {
  const [orders, setOrders] = useState([])
  const [statusFilter, setStatusFilter] = useState('all')
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
      setOrders(data)
      setError('')
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  const filteredOrders = useMemo(() => {
    if (statusFilter === 'all') return orders
    return orders.filter(o => (o.status || '').toLowerCase() === statusFilter)
  }, [orders, statusFilter])

  function getBadgeClass(status) {
    switch ((status || '').toLowerCase()) {
      case 'processed':
        return 'bg-success'
      case 'pending':
      case 'submitted':
        return 'bg-warning text-dark'
      case 'failed':
        return 'bg-danger'
      default:
        return 'bg-secondary'
    }
  }

  function getTotal(order) {
    return (order.items || []).reduce((sum, item) => sum + (item.price * item.quantity), 0)
  }

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Orders</h2>
        <div className="d-flex gap-2">
          <select
            className="form-select"
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
          >
            <option value="all">All Statuses</option>
            <option value="processed">Processed</option>
            <option value="pending">Pending</option>
            <option value="submitted">Submitted</option>
            <option value="failed">Failed</option>
          </select>

          <button className="btn btn-danger" onClick={loadOrders}>Refresh</button>
        </div>
      </div>

      {loading && <div className="alert alert-info">Loading orders...</div>}
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
                    <th>Status</th>
                    <th>Total</th>
                    <th>Created</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredOrders.map(order => (
                    <tr
                      key={order.id}
                      style={{ cursor: 'pointer' }}
                      onClick={() => navigate(`/orders/${order.id}`)}
                    >
                      <td>#{order.id}</td>
                      <td>{order.customerName}</td>
                      <td>
                        <span className={`badge ${getBadgeClass(order.status)}`}>
                          {order.status}
                        </span>
                      </td>
                      <td>€{getTotal(order).toFixed(2)}</td>
                      <td>{new Date(order.createdAtUtc).toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {filteredOrders.length === 0 && (
              <div className="alert alert-secondary mb-0">No matching orders found.</div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}