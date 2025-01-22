DROP PROCEDURE IF EXISTS SessionInsert;
DELIMITER $$
CREATE PROCEDURE SessionInsert(
    IN pId VARCHAR(36),
    IN pUserId INT,
    IN pVerificationId VARCHAR(36),
    IN pExpiryDate DATETIME,
    IN pComplete BOOLEAN,
    IN pRevoked BOOLEAN
)
BEGIN
    INSERT INTO Session (
        Id, 
        UserId, 
        VerificationId, 
        ExpiryDate, 
        Complete, 
        Revoked
    )
    VALUES (
        pId, 
        pUserId, 
        pVerificationId, 
        pExpiryDate, 
        pComplete, 
        pRevoked
    );
END $$
DELIMITER ;
