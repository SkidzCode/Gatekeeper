export interface SessionModel {
  id: string;
  userId: number;
  verificationId: string;
  expiryDate: Date;
  complete: boolean;
  revoked: boolean;
  createdAt: Date;
  updatedAt: Date;

  // Optional fields from joined Verification table
  verifyType?: string;
  verificationExpiryDate?: Date;
  verificationComplete?: boolean;
  verificationRevoked?: boolean;
}
