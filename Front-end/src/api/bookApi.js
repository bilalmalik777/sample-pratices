const API_BASE_URL = 'https://localhost:44341/api/books';

export const getBooks = async () => {
  const response = await fetch(`${API_BASE_URL}`);
  return await response.json();
};

export const getBook = async (id) => {
  const response = await fetch(`${API_BASE_URL}/${id}`);
  return await response.json();
};

export const createBook = async (book) => {
  const response = await fetch(API_BASE_URL, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(book),
  });
  return await response.json();
};

export const updateBook = async (book) => {
  const response = await fetch(API_BASE_URL, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(book),
  });
  return await response.json();
};

export const deleteBook = async (id) => {
  const response = await fetch(`${API_BASE_URL}/${id}`, {
    method: 'DELETE',
  });
  return await response;
};