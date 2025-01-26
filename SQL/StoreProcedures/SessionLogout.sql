DROP PROCEDURE IF EXISTS SessionLogout;
DELIMITER $$
CREATE PROCEDURE SessionLogout(
    IN pVerificationId VARCHAR(36)
)
BEGIN
    UPDATE Session
       SET Complete  = TRUE,
           UpdatedAt = CURRENT_TIMESTAMP
     WHERE VerificationId = pVerificationId;

     UPDATE Verification
        SET Complete = TRUE
        WHERE Id = pVerificationId AND 
	        Revoked = FALSE AND 
	        VerifyType = 'Refresh' AND 
	        Complete = FALSE AND
	        ExpiryDate > NOW();

END $$
DELIMITER ;



DROP PROCEDURE IF EXISTS SessionIdLogout;
DELIMITER $$
CREATE PROCEDURE SessionIdLogout(
    IN pSessionId VARCHAR(36)
)
BEGIN
    DECLARE vVerificationId VARCHAR(36);

    -- Mark the session as complete
    UPDATE Session
       SET Complete  = TRUE,
           UpdatedAt = CURRENT_TIMESTAMP
     WHERE Id = pSessionId;

    -- Retrieve the VerificationId
    SELECT VerificationId
      INTO vVerificationId
      FROM Session
     WHERE Id = pSessionId;

    -- Mark the related verification record as complete
    UPDATE Verification
       SET Complete = TRUE
     WHERE Id = vVerificationId
       AND Revoked = FALSE
       AND VerifyType = 'Refresh'
       AND Complete = FALSE
       AND ExpiryDate > NOW();
END $$
DELIMITER ;
