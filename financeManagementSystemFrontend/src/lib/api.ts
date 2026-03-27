import type { ApiError, ApiResponse } from '../types/api';
import { API_BASE_URL } from './utils';

function buildHeaders(token?: string, extra?: HeadersInit): HeadersInit {
  return {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...extra
  };
}

function normalizeErrors(errors: ApiError[] | null | undefined) {
  return errors?.flatMap((error) => error.messages.map((message) => `${error.field}: ${message}`)) ?? [];
}

function normalizeProblemDetails(payload: unknown) {
  if (!payload || typeof payload !== 'object') {
    return { message: null as string | null, details: [] as string[] };
  }

  const problem = payload as {
    message?: string;
    title?: string;
    detail?: string;
    errors?: Record<string, string[] | string>;
  };

  const details = problem.errors
    ? Object.entries(problem.errors).flatMap(([field, messages]) => {
        if (Array.isArray(messages)) {
          return messages.map((message) => `${field}: ${message}`);
        }

        if (typeof messages === 'string') {
          return [`${field}: ${messages}`];
        }

        return [];
      })
    : [];

  return {
    message: problem.message ?? problem.detail ?? problem.title ?? null,
    details
  };
}

export class ApiClient {
  constructor(private readonly getToken: () => string | null) {}

  async request<T>(path: string, init?: RequestInit): Promise<T> {
    const response = await fetch(`${API_BASE_URL}${path}`, {
      ...init,
      headers: buildHeaders(this.getToken() ?? undefined, init?.headers)
    });

    const payload = (await response.json().catch(() => null)) as ApiResponse<T> | Record<string, unknown> | null;

    if (!response.ok) {
      const apiPayload = payload as ApiResponse<T> | null;
      const problem = normalizeProblemDetails(payload);
      const message = apiPayload?.message ?? problem.message ?? response.statusText ?? 'Request failed';
      const details = normalizeErrors(apiPayload?.errors) ?? [];
      const mergedDetails = details.length > 0 ? details : problem.details;
      throw new Error(mergedDetails.length > 0 ? `${message} ${mergedDetails.join(' | ')}` : message);
    }

    const apiPayload = payload as ApiResponse<T> | null;
    if (!apiPayload?.success) {
      const problem = normalizeProblemDetails(payload);
      const message = apiPayload?.message ?? problem.message ?? 'Request failed';
      const details = normalizeErrors(apiPayload?.errors);
      const mergedDetails = details.length > 0 ? details : problem.details;
      throw new Error(mergedDetails.length > 0 ? `${message} ${mergedDetails.join(' | ')}` : message);
    }

    return apiPayload.data;
  }

  get<T>(path: string) {
    return this.request<T>(path, { method: 'GET' });
  }

  post<T>(path: string, body?: unknown) {
    return this.request<T>(path, {
      method: 'POST',
      body: body === undefined ? undefined : JSON.stringify(body)
    });
  }

  put<T>(path: string, body?: unknown) {
    return this.request<T>(path, {
      method: 'PUT',
      body: body === undefined ? undefined : JSON.stringify(body)
    });
  }

  delete<T>(path: string) {
    return this.request<T>(path, { method: 'DELETE' });
  }
}
