DROP PROCEDURE IF EXISTS VerificationInsert;
DELIMITER //

CREATE PROCEDURE VerificationInsert(
	IN p_Id VARCHAR(36),
	IN p_VerifyType VARCHAR(20),
    IN p_UserId INT,
    IN p_HashedToken VARCHAR(255),
    IN p_Salt VARCHAR(255),
    IN p_ExpiryDate DATETIME
)
BEGIN
    INSERT INTO Verification (Id,
        VerifyType,
        UserId,
        HashedToken,
        Salt,
        ExpiryDate,
        CreatedAt
    )
    VALUES (p_Id,
	    p_VerifyType,
        p_UserId,
        p_HashedToken,
        p_Salt,
        p_ExpiryDate,
        NOW()
    );
END //

DELIMITER ;