import {
    getBooks,
    getBook,
    createBook,
    updateBook,
    deleteBook,
  } from '../api/bookApi';
  
  export const fetchAllBooks = async () => {
    try {
      return await getBooks();
    } catch (error) {
      console.error('Error fetching books:', error);
      throw error;
    }
  };
  
  export const fetchBook = async (id) => {
    try {
      return await getBook(id);
    } catch (error) {
      console.error(`Error fetching book with id ${id}:`, error);
      throw error;
    }
  };
  
  export const addBook = async (bookData) => {
    try {
      return await createBook(bookData);
    } catch (error) {
      console.error('Error adding book:', error);
      throw error;
    }
  };
  
  export const editBook = async (bookData) => {
    try {
      return await updateBook(bookData);
    } catch (error) {
      console.error('Error updating book:', error);
      throw error;
    }
  };
  
  export const removeBook = async (id) => {
    try {
      return await deleteBook(id);
    } catch (error) {
      console.error('Error deleting book:', error);
      throw error;
    }
  };