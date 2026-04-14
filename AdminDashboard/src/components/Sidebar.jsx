import { NavLink } from 'react-router-dom'

export default function Sidebar() {
  return (
    <aside className="admin-sidebar">
      <div className="admin-sidebar-header">
        <h4 className="mb-0">AdminDashboard</h4>
      </div>

      <nav className="nav flex-column gap-2">
        <NavLink
          to="/dashboard"
          className={({ isActive }) => `admin-nav-link ${isActive ? 'active' : ''}`}
        >
          Dashboard
        </NavLink>

        <NavLink
          to="/orders"
          className={({ isActive }) => `admin-nav-link ${isActive ? 'active' : ''}`}
        >
          Orders
        </NavLink>

        <NavLink
          to="/failed-orders"
          className={({ isActive }) => `admin-nav-link ${isActive ? 'active' : ''}`}
        >
          Failed Orders
        </NavLink>
      </nav>
    </aside>
  )
}