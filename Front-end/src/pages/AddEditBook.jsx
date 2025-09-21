import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { fetchBook, addBook, editBook } from '../services/bookService';

const AddEditBook = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [book, setBook] = useState({
    title: '',
    isbn: '',
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (id) {
      const loadBook = async () => {
        try {
          setLoading(true);
          const data = await fetchBook(id);
          setBook(data);
        } catch (err) {
          setError(err.message);
        } finally {
          setLoading(false);
        }
      };
      loadBook();
    }
  }, [id]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setBook((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      setLoading(true);
      if (id) {
        await editBook({ ...book, id: parseInt(id) });
      } else {
        await addBook(book);
      }
      navigate('/books');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="loading">Loading...</div>;
  if (error) return <div className="error">Error: {error}</div>;

  return (
    <div className="book-form-container">
      <h1>{id ? 'Edit Book' : 'Add New Book'}</h1>
      <form onSubmit={handleSubmit} className="book-form">
        <div className="form-group">
          <label htmlFor="title">Title</label>
          <input
            type="text"
            id="title"
            name="title"
            value={book.title}
            onChange={handleChange}
            required
          />
        </div>
        <div className="form-group">
          <label htmlFor="isbn">ISBN</label>
          <input
            type="text"
            id="isbn"
            name="isbn"
            value={book.isbn}
            onChange={handleChange}
            required
          />
        </div>
        <div className="form-actions">
          <button
            type="button"
            className="btn cancel"
            onClick={() => navigate('/books')}
          >
            Cancel
          </button>
          <button type="submit" className="btn submit">
            {id ? 'Update' : 'Save'}
          </button>
        </div>
      </form>
    </div>
  );
};

export default AddEditBook;