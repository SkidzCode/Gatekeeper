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
