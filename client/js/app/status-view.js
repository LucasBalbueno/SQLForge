// Cuida da exibir mensagens de status na interface do usuário
export function status(message, error = false) {
  document.getElementById("status").textContent = message;
  document.getElementById("status").classList.toggle("error", error);
}
