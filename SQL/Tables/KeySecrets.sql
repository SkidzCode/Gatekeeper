CREATE TABLE IF NOT EXISTS `KeySecrets` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `SecretKey` VARBINARY(512) NOT NULL,     -- Encrypted data
    `CreatedDate` DATETIME NOT NULL,
    `ExpirationDate` DATETIME NOT NULL,
    `IsActive` TINYINT NOT NULL DEFAULT 1, -- 1=true, 0=false
    PRIMARY KEY (`Id`)
);
