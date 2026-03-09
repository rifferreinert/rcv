import axios, { type AxiosError } from 'axios'

const LOGIN_PATH = '/login'

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5041',
  withCredentials: true,
})

client.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      window.location.href = LOGIN_PATH
    }
    return Promise.reject(error)
  }
)

export default client
