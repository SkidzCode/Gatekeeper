DROP PROCEDURE IF EXISTS SessionInsert;
DELIMITER $$
CREATE PROCEDURE SessionInsert(
    IN pId VARCHAR(36),
    IN pUserId INT,
    IN pVerificationId VARCHAR(36),
    IN pExpiryDate DATETIME,
    IN pComplete BOOLEAN,
    IN pRevoked BOOLEAN,
    IN pIpAddress VARCHAR(45),
    IN pUserAgent VARCHAR(255),
    IN pSessionData TEXT
)
BEGIN
    INSERT INTO Session (
        Id, 
        UserId, 
        VerificationId, 
        ExpiryDate, 
        Complete, 
        Revoked,
        IpAddress,
        UserAgent,
        SessionData
    )
    VALUES (
        pId, 
        pUserId, 
        pVerificationId, 
        pExpiryDate, 
        pComplete, 
        pRevoked,
        pIpAddress,
        pUserAgent,
        pSessionData
    );
END $$
DELIMITER ;
