import axios, { type AxiosError } from 'axios'

const LOGIN_PATH = '/login'

export const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5041'

const client = axios.create({
  baseURL: API_BASE_URL,
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
