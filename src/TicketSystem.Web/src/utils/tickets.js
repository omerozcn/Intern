export const TICKET_STATUS = Object.freeze({
  PENDING: "pending",
  IN_PROGRESS: "inProgress",
  COMPLETED: "completed",
});

const legacyStatuses = {
  1: TICKET_STATUS.PENDING,
  2: TICKET_STATUS.IN_PROGRESS,
  3: TICKET_STATUS.COMPLETED,
};

export function normalizeTicketStatus(status) {
  if (typeof status === "number" || /^\d$/.test(String(status ?? ""))) {
    return legacyStatuses[Number(status)] ?? TICKET_STATUS.PENDING;
  }

  const compact = String(status ?? "")
    .trim()
    .replace(/[\s_-]/g, "")
    .toLowerCase();

  if (compact === "inprogress") return TICKET_STATUS.IN_PROGRESS;
  if (compact === "completed") return TICKET_STATUS.COMPLETED;
  return TICKET_STATUS.PENDING;
}

export function normalizeTicket(ticket) {
  return {
    ...ticket,
    id: Number(ticket.id),
    status: normalizeTicketStatus(ticket.status),
    description: ticket.description ?? "",
    answer: ticket.answer ?? "",
    productName: ticket.productName ?? "",
    firmName: ticket.firmName ?? "",
    createdBy: ticket.createdBy ?? "",
    created: ticket.created ?? null,
    updated: ticket.updated ?? null,
    newProduct: Boolean(ticket.newProduct),
  };
}

export function normalizeTicketCounts(rows = []) {
  const counts = {
    [TICKET_STATUS.PENDING]: 0,
    [TICKET_STATUS.IN_PROGRESS]: 0,
    [TICKET_STATUS.COMPLETED]: 0,
  };

  for (const row of rows) {
    counts[normalizeTicketStatus(row.status)] = Number(row.count) || 0;
  }

  return {
    ...counts,
    total: Object.values(counts).reduce((sum, count) => sum + count, 0),
  };
}
