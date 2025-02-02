DROP PROCEDURE IF EXISTS SessionRefresh;
DELIMITER $$
CREATE PROCEDURE SessionRefresh(
    IN pUserId INT,
    IN pOldVerificationId VARCHAR(36),
    IN pNewVerificationId VARCHAR(36),
    OUT pSessionId VARCHAR(36)
)
BEGIN
    DECLARE vSessionId VARCHAR(36);

    UPDATE Session
       SET VerificationId = pNewVerificationId,
           UpdatedAt      = CURRENT_TIMESTAMP
     WHERE UserId = pUserId
       AND VerificationId = pOldVerificationId
       AND Complete = 0
       AND Revoked = 0
       AND ExpiryDate > NOW();

    SELECT Id INTO vSessionId
      FROM Session
     WHERE UserId = pUserId
       AND VerificationId = pNewVerificationId;

    SET pSessionId = vSessionId;
END $$
DELIMITER ;
