import Sidebar from './Sidebar'
import Topbar from './Topbar'

export default function AdminLayout({ children }) {
  return (
    <div className="admin-page">
      <Sidebar />
      <div className="admin-main">
        <Topbar />
        <div className="admin-content container-fluid py-4">
          {children}
        </div>
      </div>
    </div>
  )
}