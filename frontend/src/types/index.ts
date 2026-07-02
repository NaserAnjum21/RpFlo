export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  department: string;
}

export type UserRole = 'Requester' | 'Manager' | 'Finance' | 'Admin';

export type ProcurementStatus =
  | 'Draft'
  | 'Submitted'
  | 'ManagerApproved'
  | 'ManagerRejected'
  | 'FinanceApproved'
  | 'FinanceRejected'
  | 'PurchaseOrderIssued';

export interface ProcurementListItem {
  id: string;
  title: string;
  department: string;
  urgency: string;
  status: ProcurementStatus;
  totalAmount: number;
  currency: string;
  requesterName: string;
  createdAt: string;
  updatedAt: string;
}

export interface ProcurementResponse {
  id: string;
  title: string;
  description: string;
  department: string;
  urgency: string;
  status: ProcurementStatus;
  totalAmount: number;
  currency: string;
  poNumber: string | null;
  requester: RequesterInfo;
  lineItems: LineItemResponse[];
  auditTrail: AuditEntryResponse[];
  comments: CommentResponse[];
  createdAt: string;
  updatedAt: string;
}

export interface RequesterInfo {
  id: string;
  name: string;
  email: string;
  department: string;
}

export interface LineItemResponse {
  id: string;
  name: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface AuditEntryResponse {
  id: string;
  userId: string;
  userName: string;
  action: string;
  fromStatus: string;
  toStatus: string;
  comment: string | null;
  createdAt: string;
}

export interface CommentResponse {
  id: string;
  userId: string;
  userName: string;
  text: string;
  createdAt: string;
}

export interface DashboardMetrics {
  totalRequests: number;
  draftCount: number;
  pendingApprovalCount: number;
  approvedCount: number;
  rejectedCount: number;
  purchaseOrderCount: number;
  totalApprovedAmount: number;
  averageProcessingTimeHours: number;
  statusBreakdown: { status: string; count: number }[];
  departmentBreakdown: { department: string; count: number; totalAmount: number }[];
}

export interface NotificationResponse {
  id: string;
  title: string;
  message: string;
  referenceId: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface CreateProcurementRequest {
  title: string;
  description: string;
  department: string;
  urgency: string;
  lineItems: { name: string; quantity: number; unitPrice: number }[];
}
