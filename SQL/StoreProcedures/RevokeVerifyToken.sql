DROP PROCEDURE IF EXISTS RevokeVerifyToken;

DELIMITER //
CREATE PROCEDURE RevokeVerifyToken(
    IN p_UserId INT,
    IN p_TokenId VARCHAR(36),
    IN p_VerifyType VARCHAR(20),
    OUT p_RowsAffected INT
)
BEGIN
    IF p_TokenId IS NOT NULL THEN
        UPDATE Verification
        SET Revoked = TRUE
        WHERE Id = p_TokenId AND 
	        UserId = p_UserId AND 
	        Revoked = FALSE AND 
	        VerifyType = p_VerifyType AND 
	        Complete = FALSE AND
	        ExpiryDate > NOW();
    ELSE
        UPDATE Verification
        SET Revoked = TRUE
        WHERE UserId = p_UserId AND 
	        UserId = p_UserId AND 
	        Revoked = FALSE AND 
	        VerifyType = p_VerifyType AND 
	        Complete = FALSE AND
	        ExpiryDate > NOW();
    END IF;

    -- Get the number of rows affected by the update statement
    SET p_RowsAffected = ROW_COUNT();
END //
DELIMITER ;