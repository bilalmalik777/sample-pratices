import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { fetchBook } from '../services/bookService';

const BookDetails = () => {
  const { id } = useParams();
  const [book, setBook] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const loadBook = async () => {
      try {
        const data = await fetchBook(id);
        setBook(data);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };

    loadBook();
  }, [id]);

  if (loading) return <div className="loading">Loading...</div>;
  if (error) return <div className="error">Error: {error}</div>;
  if (!book) return <div className="error">Book not found</div>;

  return (
    <div className="book-details">
      <h1>{book.title}</h1>
      <div className="detail-item">
        <strong>ID:</strong> {book.id}
      </div>
      <div className="detail-item">
        <strong>ISBN:</strong> {book.isbn}
      </div>
      <div className="action-buttons">
        <Link to={`/books/edit/${book.id}`} className="btn edit">
          Edit
        </Link>
        <Link to="/books" className="btn back">
          Back to List
        </Link>
      </div>
    </div>
  );
};

export default BookDetails;