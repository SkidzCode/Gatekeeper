DROP PROCEDURE IF EXISTS SessionActiveListForUser;
DELIMITER $$
CREATE PROCEDURE SessionActiveListForUser(
    IN pUserId INT
)
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
           V.Complete   AS VerificationComplete,
           V.Revoked    AS VerificationRevoked,
           S.IpAddress,
           S.UserAgent,
           S.SessionData
      FROM Session S
      JOIN Verification V ON S.VerificationId = V.Id
     WHERE S.UserId = pUserId
       AND S.Revoked = 0
       AND S.Complete = 0
       AND S.ExpiryDate > NOW()
       AND V.Revoked = 0
       AND V.Complete = 0
       AND V.ExpiryDate > NOW();
END $$
DELIMITER ;
