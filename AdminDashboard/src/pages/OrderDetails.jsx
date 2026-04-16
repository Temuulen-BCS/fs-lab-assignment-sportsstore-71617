import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { getOrderById } from '../services/api'

const SERVICE_OPTIONS = [
  { value: 'overview', label: 'Overview' },
  { value: 'messageconsumer', label: 'MessageConsumer' },
  { value: 'inventoryservice', label: 'InventoryService' },
  { value: 'paymentworkflowservice', label: 'PaymentworkflowService' },
  { value: 'shippingservice', label: 'ShippingService' },
]

export default function OrderDetails() {
  const { id } = useParams()
  const [order, setOrder] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [selectedService, setSelectedService] = useState('overview')

  useEffect(() => {
    loadOrder()
  }, [id])

  async function loadOrder() {
    try {
      setLoading(true)
      const data = await getOrderById(id)
      setOrder(data)
      setError('')
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  function getBadgeClass(status) {
    switch ((status || '').toLowerCase()) {
      case 'completed':
      case 'processed':
      case 'payment approved':
      case 'inventory confirmed':
        return 'bg-success'
      case 'processing':
      case 'pending':
      case 'submitted':
        return 'bg-warning text-dark'
      case 'failed':
        return 'bg-danger'
      default:
        return 'bg-secondary'
    }
  }

  function getTotal(items) {
    return (items || []).reduce((sum, item) => sum + (item.price * item.quantity), 0)
  }

  function formatCurrency(value) {
    return new Intl.NumberFormat('en-IE', {
      style: 'currency',
      currency: 'EUR',
    }).format(value || 0)
  }

  function getServiceLogs() {
    const logs = order?.serviceLogs || []
    return logs.filter((entry) => (entry.service || '').toLowerCase() === selectedService)
  }

  const serviceLogs = getServiceLogs()

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Order Details</h2>
        <button className="btn btn-danger" onClick={loadOrder}>Refresh</button>
      </div>

      {loading && <div className="alert alert-info">Loading order details...</div>}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && order && (
        <div className="card shadow-sm border-0">
          <div className="card-body">
            <h4 className="mb-3">Order #{order.id}</h4>

            <p>
              <strong>Status:</strong>{' '}
              <span className={`badge ${getBadgeClass(order.status)}`}>
                {order.status}
              </span>
            </p>

            <p><strong>Customer:</strong> {order.customerName}</p>
            <p><strong>Address:</strong> {order.address}, {order.city}, {order.country}</p>
            <p><strong>Created:</strong> {new Date(order.createdAtUtc).toLocaleString()}</p>

            <div className="table-responsive mt-4">
              <table className="table table-hover align-middle">
                <thead className="table-light">
                  <tr>
                    <th>Product</th>
                    <th>Qty</th>
                    <th>Price</th>
                    <th>Subtotal</th>
                  </tr>
                </thead>
                <tbody>
                  {(order.items || []).map((item, index) => (
                    <tr key={index}>
                      <td>{item.productName}</td>
                      <td>{item.quantity}</td>
                      <td>{formatCurrency(item.price)}</td>
                      <td>{formatCurrency(item.price * item.quantity)}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr className="table-light">
                    <th colSpan="3" className="text-end">Total</th>
                    <th>{formatCurrency(getTotal(order.items))}</th>
                  </tr>
                </tfoot>
              </table>
            </div>

            <div className="mt-4">
              <div className="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-3">
                <h5 className="mb-0">Operational View</h5>
                <select
                  className="form-select w-auto"
                  value={selectedService}
                  onChange={(event) => setSelectedService(event.target.value)}
                >
                  {SERVICE_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </div>

              <div className="row g-3 mb-3">
                <div className="col-md-4">
                  <div className="border rounded p-3 bg-light h-100">
                    <div className="text-muted small">Inventory</div>
                    <div className="fw-semibold">{order.inventoryStatus || 'N/A'}</div>
                  </div>
                </div>
                <div className="col-md-4">
                  <div className="border rounded p-3 bg-light h-100">
                    <div className="text-muted small">Payment</div>
                    <div className="fw-semibold">{order.paymentStatus || 'N/A'}</div>
                  </div>
                </div>
                <div className="col-md-4">
                  <div className="border rounded p-3 bg-light h-100">
                    <div className="text-muted small">Shipping</div>
                    <div className="fw-semibold">
                      {order.shippingStatus || 'N/A'}
                      {order.shipmentReference ? ` (${order.shipmentReference})` : ''}
                    </div>
                  </div>
                </div>
              </div>

              {serviceLogs.length > 0 ? (
                <div className="list-group">
                  {serviceLogs.map((entry, index) => (
                    <div key={`${entry.timestampUtc}-${index}`} className="list-group-item">
                      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-1">
                        <strong>{entry.service}</strong>
                        <span className="badge text-bg-dark">{entry.level}</span>
                      </div>
                      <div>{entry.message}</div>
                      <small className="text-muted">
                        {new Date(entry.timestampUtc).toLocaleString()}
                      </small>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="alert alert-secondary mb-0">
                  No log entries are available for this service yet.
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

