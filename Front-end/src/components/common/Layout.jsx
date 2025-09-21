import React from 'react';
import { Link } from 'react-router-dom';

const Layout = ({ children }) => {
  return (
    <div className="layout">
      <header className="header">
        <nav>
          <ul className="nav-list">
            <li>
              <Link to="/">Home</Link>
            </li>
            <li>
              <Link to="/books/add">Add Book</Link>
            </li>
          </ul>
        </nav>
      </header>
      <main className="main-content">{children}</main>
      <footer className="footer">
        <p>Book Management System © {new Date().getFullYear()}</p>
      </footer>
    </div>
  );
};

export default Layout;