import { useEffect, useState } from 'react'
import { getOrders } from '../services/api'

export default function Dashboard() {
  const [orders, setOrders] = useState([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

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

  const processed = orders.filter(o => o.status?.toLowerCase() === 'processed').length
  const pending = orders.filter(o =>
    o.status?.toLowerCase() === 'pending' ||
    o.status?.toLowerCase() === 'submitted'
  ).length
  const failed = orders.filter(o => o.status?.toLowerCase() === 'failed').length

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Dashboard</h2>
        <button className="btn btn-danger" onClick={loadOrders}>Refresh</button>
      </div>

      {loading && <div className="alert alert-info">Loading dashboard...</div>}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && (
        <div className="row g-4">
          <div className="col-md-3">
            <div className="card shadow-sm border-0 dashboard-card">
              <div className="card-body">
                <h5>Total Orders</h5>
                <h2>{orders.length}</h2>
              </div>
            </div>
          </div>

          <div className="col-md-3">
            <div className="card shadow-sm border-0 dashboard-card">
              <div className="card-body">
                <h5>Processed</h5>
                <h2>{processed}</h2>
              </div>
            </div>
          </div>

          <div className="col-md-3">
            <div className="card shadow-sm border-0 dashboard-card">
              <div className="card-body">
                <h5>Pending</h5>
                <h2>{pending}</h2>
              </div>
            </div>
          </div>

          <div className="col-md-3">
            <div className="card shadow-sm border-0 dashboard-card">
              <div className="card-body">
                <h5>Failed</h5>
                <h2>{failed}</h2>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}