DROP PROCEDURE IF EXISTS SessionListMostRecentActivity;
DELIMITER $$
CREATE PROCEDURE SessionListMostRecentActivity()
BEGIN
    SELECT S.Id,
           S.UserId,
           S.VerificationId,
           S.ExpiryDate,
           S.Complete,
           S.Revoked,
           S.CreatedAt,
           S.UpdatedAt,
           V.VerifyType,
           V.ExpiryDate AS VerificationExpiryDate,
           S.IpAddress,
           S.UserAgent,
           S.SessionData
      FROM Session S
      JOIN Verification V ON S.VerificationId = V.Id
     WHERE V.VerifyType = 'Refresh'
       AND S.UpdatedAt >= DATE_SUB(NOW(), INTERVAL 15 MINUTE)
     ORDER BY S.UpdatedAt DESC;
END $$
DELIMITER ;
