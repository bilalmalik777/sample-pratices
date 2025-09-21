import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { fetchAllBooks, removeBook } from '../services/bookService';

const BookList = () => {
  const [books, setBooks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const loadBooks = async () => {
      try {
        const data = await fetchAllBooks();
        setBooks(data);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };

    loadBooks();
  }, []);

  const handleDelete = async (id) => {
    try {
      debugger;

      await removeBook(id);
      setBooks(books.filter(book => book.id !== id));
    } catch (err) {
      setError(err.message);
    }
  };

  if (loading) return <div className="loading">Loading...</div>;
  if (error) return <div className="error">Error: {error}</div>;

  return (
    <div className="book-list">
      <h1>Book List</h1>
      <div className="book-grid">
        {books.map((book) => (
          <div key={book.id} className="book-card">
            <h3>{book.title}</h3>
            <p>ISBN: {book.isbn}</p>
            <div className="book-actions">
              <Link to={`/books/${book.id}`} className="btn view">View</Link>
              <Link to={`/books/edit/${book.id}`} className="btn edit">Edit</Link>
              <button onClick={() => handleDelete(book.id)} className="btn delete">
                Delete
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default BookList;