import { API_URL } from "../config/index.js";

// GET /api/query/metadata
export function getMetadata(signal) {
  return request("metadata", { method: "GET", signal });
}

// POST /api/query/generate
export function generateQuery(query, signal) {
  return request("generate", { method: "POST", body: query, signal });
}

// POST /api/query/execute
export function executeQuery(query, signal) {
  return request("execute", { method: "POST", body: query, signal });
}

// Função que monta a requisição para a API, tratando body e a resposta
async function request(path, { method, body, signal }) {
  const response = await fetch(`${API_URL}/${path}`, {
    method,
    signal,
    ...(body !== undefined && {
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    }),
  });

  const data = await response.json();

  if (!response.ok) {
    const errors = data.errors
      ? Object.values(data.errors).flat().join(" ")
      : "";
    throw new Error(
      data.detail ||
        errors ||
        data.title ||
        "Não foi possível concluir a operação.",
    );
  }
  return data;
}

