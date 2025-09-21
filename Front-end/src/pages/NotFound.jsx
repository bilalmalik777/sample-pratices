import React from 'react';
import { Link } from 'react-router-dom';

const NotFound = () => {
  return (
    <div className="not-found">
      <h1>404 - Page Not Found</h1>
      <p>The page you are looking for doesn't exist or has been moved.</p>
      <div className="not-found-actions">
        <Link to="/" className="btn home">
          Go to Homepage
        </Link>
      </div>
    </div>
  );
};

export default NotFound;