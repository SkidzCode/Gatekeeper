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
END $$
DELIMITER ;
