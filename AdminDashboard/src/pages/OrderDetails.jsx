import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { getOrderById } from '../services/api'

export default function OrderDetails() {
  const { id } = useParams()
  const [order, setOrder] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

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

  function getTotal(items) {
    return (items || []).reduce((sum, item) => sum + (item.price * item.quantity), 0)
  }

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
                      <td>€{item.price.toFixed(2)}</td>
                      <td>€{(item.price * item.quantity).toFixed(2)}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr className="table-light">
                    <th colSpan="3" className="text-end">Total</th>
                    <th>€{getTotal(order.items).toFixed(2)}</th>
                  </tr>
                </tfoot>
              </table>
            </div>

            <div className="mt-4">
              <h5>Operational View</h5>
              <p><strong>Payment Status:</strong> {order.paymentStatus || 'N/A'}</p>
              <p><strong>Inventory Result:</strong> {order.inventoryStatus || 'N/A'}</p>
              <p><strong>Shipment Status:</strong> {order.shippingStatus || 'N/A'}</p>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}