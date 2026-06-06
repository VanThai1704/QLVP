/* =============================================================================
   Office Management Database (English schema, production-oriented design)
   Replaces legacy Vietnamese schema (QuanLyVanPhong).

   Entity flow:
     Account -> Employee | Tenant
     Tenant + Office + Employee -> Contract -> Invoice -> InvoiceDetail
     Office + ServiceType -> OfficeService (meters / recurring services)
     Tenant + Office + Employee -> MaintenanceRequest
   ============================================================================= */

USE master;
GO

IF DB_ID(N'OfficeManagement') IS NOT NULL
BEGIN
    ALTER DATABASE OfficeManagement SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE OfficeManagement;
END
GO

CREATE DATABASE OfficeManagement;
GO

USE OfficeManagement;
GO

/* ---------------------------------------------------------------------------
   TABLES
   --------------------------------------------------------------------------- */

CREATE TABLE dbo.Account (
    Id            INT            IDENTITY(1,1) NOT NULL,
    Username      VARCHAR(50)    NOT NULL,
    PasswordHash  VARCHAR(256)   NOT NULL,
    Role          NVARCHAR(30)   NOT NULL,
    Status        NVARCHAR(20)   NOT NULL CONSTRAINT DF_Account_Status DEFAULT N'Active',
    CreatedAt     DATETIME2(0)   NOT NULL CONSTRAINT DF_Account_CreatedAt DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Account PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Account_Username UNIQUE (Username),
    CONSTRAINT CK_Account_Role CHECK (Role IN (N'Tenant', N'Employee', N'Admin')),
    CONSTRAINT CK_Account_Status CHECK (Status IN (N'Active', N'Inactive', N'Locked'))
);

CREATE TABLE dbo.Employee (
    Id        INT           IDENTITY(1,1) NOT NULL,
    AccountId INT           NOT NULL,
    FullName  NVARCHAR(100) NOT NULL,
    Phone     VARCHAR(15)   NULL,
    Email     VARCHAR(100)  NULL,
    Position  NVARCHAR(50)  NULL,

    CONSTRAINT PK_Employee PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Employee_AccountId UNIQUE (AccountId),
    CONSTRAINT UQ_Employee_Email UNIQUE (Email),
    CONSTRAINT FK_Employee_Account FOREIGN KEY (AccountId)
        REFERENCES dbo.Account (Id)
);

CREATE TABLE dbo.Tenant (
    Id                  INT           IDENTITY(1,1) NOT NULL,
    AccountId           INT           NOT NULL,
    CompanyName         NVARCHAR(100) NOT NULL,
    RepresentativeName  NVARCHAR(100) NOT NULL,
    Phone               VARCHAR(15)   NULL,
    Email               VARCHAR(100)  NULL,
    Address             NVARCHAR(200) NULL,

    CONSTRAINT PK_Tenant PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Tenant_AccountId UNIQUE (AccountId),
    CONSTRAINT UQ_Tenant_Email UNIQUE (Email),
    CONSTRAINT FK_Tenant_Account FOREIGN KEY (AccountId)
        REFERENCES dbo.Account (Id)
);

CREATE TABLE dbo.Office (
    Id          INT            IDENTITY(1,1) NOT NULL,
    OfficeCode  VARCHAR(10)    NOT NULL,
    RoomNumber  NVARCHAR(20)   NOT NULL,
    Name        NVARCHAR(100)  NOT NULL,
    AreaSqm     DECIMAL(10, 2) NOT NULL,
    Capacity    INT            NOT NULL,
    Location    NVARCHAR(100)  NULL,
    MonthlyRent DECIMAL(18, 2) NOT NULL,
    Status      NVARCHAR(30)   NOT NULL CONSTRAINT DF_Office_Status DEFAULT N'Available',
    Description NVARCHAR(500)  NULL,

    CONSTRAINT PK_Office PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Office_OfficeCode UNIQUE (OfficeCode),
    CONSTRAINT UQ_Office_RoomNumber UNIQUE (RoomNumber),
    CONSTRAINT CK_Office_AreaSqm CHECK (AreaSqm > 0),
    CONSTRAINT CK_Office_Capacity CHECK (Capacity > 0),
    CONSTRAINT CK_Office_MonthlyRent CHECK (MonthlyRent >= 0),
    CONSTRAINT CK_Office_Status CHECK (Status IN (N'Available', N'Rented', N'Maintenance'))
);

CREATE TABLE dbo.Contract (
    Id                    INT            IDENTITY(1,1) NOT NULL,
    ContractCode          VARCHAR(10)    NOT NULL,
    SignedDate            DATE           NOT NULL,
    StartDate             DATE           NOT NULL,
    EndDate               DATE           NOT NULL,
    DepositAmount         DECIMAL(18, 2) NOT NULL CONSTRAINT DF_Contract_Deposit DEFAULT 0,
    MonthlyRent           DECIMAL(18, 2) NOT NULL,
    Terms                 NVARCHAR(500)  NULL,
    Status                NVARCHAR(30)   NOT NULL CONSTRAINT DF_Contract_Status DEFAULT N'Active',
    TenantId              INT            NOT NULL,
    OfficeId              INT            NOT NULL,
    CreatedByEmployeeId   INT            NOT NULL,

    CONSTRAINT PK_Contract PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Contract_ContractCode UNIQUE (ContractCode),
    CONSTRAINT CK_Contract_Dates CHECK (EndDate > StartDate),
    CONSTRAINT CK_Contract_Deposit CHECK (DepositAmount >= 0),
    CONSTRAINT CK_Contract_MonthlyRent CHECK (MonthlyRent >= 0),
    CONSTRAINT CK_Contract_Status CHECK (Status IN (N'Active', N'Expired', N'Terminated')),
    CONSTRAINT FK_Contract_Tenant FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenant (Id),
    CONSTRAINT FK_Contract_Office FOREIGN KEY (OfficeId)
        REFERENCES dbo.Office (Id),
    CONSTRAINT FK_Contract_Employee FOREIGN KEY (CreatedByEmployeeId)
        REFERENCES dbo.Employee (Id)
);

CREATE TABLE dbo.ServiceType (
    Id               INT            IDENTITY(1,1) NOT NULL,
    Name             NVARCHAR(100)  NOT NULL,
    Unit             NVARCHAR(20)   NOT NULL,
    DefaultUnitPrice DECIMAL(18, 2) NOT NULL,
    IsMetered        BIT            NOT NULL CONSTRAINT DF_ServiceType_IsMetered DEFAULT 1,

    CONSTRAINT PK_ServiceType PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_ServiceType_Name UNIQUE (Name),
    CONSTRAINT CK_ServiceType_DefaultUnitPrice CHECK (DefaultUnitPrice >= 0)
);

CREATE TABLE dbo.OfficeService (
    Id            INT            IDENTITY(1,1) NOT NULL,
    OfficeId      INT            NOT NULL,
    ServiceTypeId INT            NOT NULL,
    UnitPrice     DECIMAL(18, 2) NOT NULL,
    IsActive      BIT            NOT NULL CONSTRAINT DF_OfficeService_IsActive DEFAULT 1,

    CONSTRAINT PK_OfficeService PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_OfficeService_Office_Service UNIQUE (OfficeId, ServiceTypeId),
    CONSTRAINT CK_OfficeService_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT FK_OfficeService_Office FOREIGN KEY (OfficeId)
        REFERENCES dbo.Office (Id),
    CONSTRAINT FK_OfficeService_ServiceType FOREIGN KEY (ServiceTypeId)
        REFERENCES dbo.ServiceType (Id)
);

CREATE TABLE dbo.Invoice (
    Id               INT            IDENTITY(1,1) NOT NULL,
    InvoiceCode      VARCHAR(10)    NOT NULL,
    ContractId       INT            NOT NULL,
    BillingMonth     TINYINT        NOT NULL,
    BillingYear      SMALLINT       NOT NULL,
    IssueDate        DATE           NOT NULL,
    RentAmount       DECIMAL(18, 2) NOT NULL,
    ServicesSubtotal DECIMAL(18, 2) NOT NULL CONSTRAINT DF_Invoice_ServicesSubtotal DEFAULT 0,
    TotalAmount      DECIMAL(18, 2) NOT NULL,
    Status           NVARCHAR(30)   NOT NULL CONSTRAINT DF_Invoice_Status DEFAULT N'Unpaid',
    PaidDate         DATE           NULL,

    CONSTRAINT PK_Invoice PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Invoice_InvoiceCode UNIQUE (InvoiceCode),
    CONSTRAINT UQ_Invoice_Contract_Period UNIQUE (ContractId, BillingMonth, BillingYear),
    CONSTRAINT CK_Invoice_BillingMonth CHECK (BillingMonth BETWEEN 1 AND 12),
    CONSTRAINT CK_Invoice_BillingYear CHECK (BillingYear >= 2000),
    CONSTRAINT CK_Invoice_RentAmount CHECK (RentAmount >= 0),
    CONSTRAINT CK_Invoice_TotalAmount CHECK (TotalAmount >= 0),
    CONSTRAINT CK_Invoice_Status CHECK (Status IN (N'Unpaid', N'Paid', N'Overdue', N'Cancelled')),
    CONSTRAINT FK_Invoice_Contract FOREIGN KEY (ContractId)
        REFERENCES dbo.Contract (Id)
);

CREATE TABLE dbo.InvoiceDetail (
    Id              INT            IDENTITY(1,1) NOT NULL,
    InvoiceId       INT            NOT NULL,
    OfficeServiceId INT            NOT NULL,
    PreviousReading DECIMAL(18, 2) NOT NULL CONSTRAINT DF_InvoiceDetail_PreviousReading DEFAULT 0,
    CurrentReading  DECIMAL(18, 2) NOT NULL,
    UnitPrice       DECIMAL(18, 2) NOT NULL,
    Quantity        AS (CurrentReading - PreviousReading) PERSISTED,
    LineTotal       AS ((CurrentReading - PreviousReading) * UnitPrice) PERSISTED,

    CONSTRAINT PK_InvoiceDetail PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_InvoiceDetail_Invoice_Service UNIQUE (InvoiceId, OfficeServiceId),
    CONSTRAINT CK_InvoiceDetail_Readings CHECK (CurrentReading >= PreviousReading),
    CONSTRAINT CK_InvoiceDetail_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT FK_InvoiceDetail_Invoice FOREIGN KEY (InvoiceId)
        REFERENCES dbo.Invoice (Id),
    CONSTRAINT FK_InvoiceDetail_OfficeService FOREIGN KEY (OfficeServiceId)
        REFERENCES dbo.OfficeService (Id)
);

CREATE TABLE dbo.MaintenanceRequest (
    Id                  INT           IDENTITY(1,1) NOT NULL,
    RequestCode         VARCHAR(10)   NOT NULL,
    OfficeId            INT           NOT NULL,
    TenantId            INT           NOT NULL,
    AssignedEmployeeId  INT           NULL,
    Description         NVARCHAR(500) NOT NULL,
    Priority            NVARCHAR(20)  NOT NULL CONSTRAINT DF_MaintenanceRequest_Priority DEFAULT N'Normal',
    Status              NVARCHAR(30)  NOT NULL CONSTRAINT DF_MaintenanceRequest_Status DEFAULT N'Pending',
    CreatedDate         DATE          NOT NULL,
    CompletedDate       DATE          NULL,

    CONSTRAINT PK_MaintenanceRequest PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_MaintenanceRequest_RequestCode UNIQUE (RequestCode),
    CONSTRAINT CK_MaintenanceRequest_Priority CHECK (Priority IN (N'Low', N'Normal', N'High', N'Urgent')),
    CONSTRAINT CK_MaintenanceRequest_Status CHECK (Status IN (N'Pending', N'InProgress', N'Completed', N'Cancelled')),
    CONSTRAINT CK_MaintenanceRequest_CompletedDate CHECK (CompletedDate IS NULL OR CompletedDate >= CreatedDate),
    CONSTRAINT FK_MaintenanceRequest_Office FOREIGN KEY (OfficeId)
        REFERENCES dbo.Office (Id),
    CONSTRAINT FK_MaintenanceRequest_Tenant FOREIGN KEY (TenantId)
        REFERENCES dbo.Tenant (Id),
    CONSTRAINT FK_MaintenanceRequest_Employee FOREIGN KEY (AssignedEmployeeId)
        REFERENCES dbo.Employee (Id)
);

GO

/* ---------------------------------------------------------------------------
   INDEXES (FK + common query paths)
   --------------------------------------------------------------------------- */

CREATE INDEX IX_Employee_AccountId ON dbo.Employee (AccountId);
CREATE INDEX IX_Tenant_AccountId ON dbo.Tenant (AccountId);

CREATE INDEX IX_Contract_TenantId ON dbo.Contract (TenantId);
CREATE INDEX IX_Contract_OfficeId ON dbo.Contract (OfficeId);
CREATE INDEX IX_Contract_CreatedByEmployeeId ON dbo.Contract (CreatedByEmployeeId);
CREATE INDEX IX_Contract_Status_Dates ON dbo.Contract (Status, StartDate, EndDate);

CREATE INDEX IX_OfficeService_OfficeId ON dbo.OfficeService (OfficeId);
CREATE INDEX IX_OfficeService_ServiceTypeId ON dbo.OfficeService (ServiceTypeId);

CREATE INDEX IX_Invoice_ContractId ON dbo.Invoice (ContractId);
CREATE INDEX IX_Invoice_Status ON dbo.Invoice (Status);
CREATE INDEX IX_Invoice_BillingPeriod ON dbo.Invoice (BillingYear, BillingMonth);

CREATE INDEX IX_InvoiceDetail_InvoiceId ON dbo.InvoiceDetail (InvoiceId);
CREATE INDEX IX_InvoiceDetail_OfficeServiceId ON dbo.InvoiceDetail (OfficeServiceId);

CREATE INDEX IX_MaintenanceRequest_OfficeId ON dbo.MaintenanceRequest (OfficeId);
CREATE INDEX IX_MaintenanceRequest_TenantId ON dbo.MaintenanceRequest (TenantId);
CREATE INDEX IX_MaintenanceRequest_AssignedEmployeeId ON dbo.MaintenanceRequest (AssignedEmployeeId);
CREATE INDEX IX_MaintenanceRequest_Status ON dbo.MaintenanceRequest (Status);

GO

/* ---------------------------------------------------------------------------
   TRIGGERS
   --------------------------------------------------------------------------- */

-- Block tenant deletion when contracts exist
CREATE TRIGGER dbo.TR_Tenant_PreventDeleteWithContracts
ON dbo.Tenant
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM deleted d
        INNER JOIN dbo.Contract c ON c.TenantId = d.Id
    )
    BEGIN
        RAISERROR(N'Cannot delete tenant with existing contracts.', 16, 1);
        RETURN;
    END;

    DELETE t
    FROM dbo.Tenant t
    INNER JOIN deleted d ON d.Id = t.Id;
END;
GO

-- Validate office availability and prevent overlapping active contracts
CREATE TRIGGER dbo.TR_Contract_ValidateAndActivate
ON dbo.Contract
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.Office o ON o.Id = i.OfficeId
        WHERE i.Status = N'Active'
          AND o.Status = N'Maintenance'
    )
    BEGIN
        RAISERROR(N'Office is under maintenance and cannot be rented.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.Contract c
            ON c.OfficeId = i.OfficeId
           AND c.Status = N'Active'
           AND c.Id <> i.Id
        WHERE i.Status = N'Active'
          AND i.StartDate <= c.EndDate
          AND i.EndDate >= c.StartDate
    )
    BEGIN
        RAISERROR(N'Office already has an overlapping active contract.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    UPDATE o
    SET Status = N'Rented'
    FROM dbo.Office o
    INNER JOIN inserted i ON i.OfficeId = o.Id
    WHERE i.Status = N'Active';

    UPDATE o
    SET Status = N'Available'
    FROM dbo.Office o
    INNER JOIN inserted i ON i.OfficeId = o.Id
    WHERE i.Status IN (N'Expired', N'Terminated')
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.Contract c
          WHERE c.OfficeId = o.Id
            AND c.Status = N'Active'
            AND c.Id <> i.Id
      );
END;
GO

-- Recalculate invoice service subtotal and total after line-item changes
CREATE TRIGGER dbo.TR_InvoiceDetail_RecalculateInvoice
ON dbo.InvoiceDetail
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH AffectedInvoices AS (
        SELECT InvoiceId FROM inserted
        UNION
        SELECT InvoiceId FROM deleted
    )
    UPDATE inv
    SET
        ServicesSubtotal = ISNULL(details.Subtotal, 0),
        TotalAmount = inv.RentAmount + ISNULL(details.Subtotal, 0)
    FROM dbo.Invoice inv
    INNER JOIN AffectedInvoices ai ON ai.InvoiceId = inv.Id
    OUTER APPLY (
        SELECT SUM(d.LineTotal) AS Subtotal
        FROM dbo.InvoiceDetail d
        WHERE d.InvoiceId = inv.Id
    ) details;
END;
GO

-- Ensure maintenance request tenant actually rents the office
CREATE TRIGGER dbo.TR_MaintenanceRequest_ValidateTenantOffice
ON dbo.MaintenanceRequest
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.Contract c
            WHERE c.TenantId = i.TenantId
              AND c.OfficeId = i.OfficeId
              AND c.Status = N'Active'
              AND i.CreatedDate BETWEEN c.StartDate AND c.EndDate
        )
    )
    BEGIN
        RAISERROR(N'Tenant has no active contract for the selected office.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;
END;
GO

/* ---------------------------------------------------------------------------
   SAMPLE DATA
   PasswordHash values are placeholders — hash passwords in the application.
   --------------------------------------------------------------------------- */

INSERT INTO dbo.Account (Username, PasswordHash, Role, Status) VALUES
(N'vy01',   N'$2a$demo$hash_for_123456', N'Tenant',   N'Active'),
(N'tuan01', N'$2a$demo$hash_for_123456', N'Tenant',   N'Active'),
(N'toan01', N'$2a$demo$hash_for_123456', N'Tenant',   N'Active'),
(N'huy01',  N'$2a$demo$hash_for_123456', N'Employee', N'Active'),
(N'thai01', N'$2a$demo$hash_for_123456', N'Employee', N'Active');

INSERT INTO dbo.Employee (AccountId, FullName, Phone, Email, Position) VALUES
(4, N'Le Quang Huy', N'0911111111', N'huy@gmail.com',  N'Manager'),
(5, N'Anh Thai',     N'0922222222', N'thai@gmail.com', N'Staff');

INSERT INTO dbo.Tenant (AccountId, CompanyName, RepresentativeName, Phone, Email, Address) VALUES
(1, N'Vy Tech Co., Ltd.',       N'Mai Ha Thanh Vy',    N'0933333333', N'vy@gmail.com',   N'Can Tho'),
(2, N'Tuan Software Co., Ltd.', N'Nguyen Minh Tuan',   N'0944444444', N'tuan@gmail.com', N'Dong Thap'),
(3, N'Toan Security Co., Ltd.', N'Ngo Le Thanh Toan',  N'0955555555', N'toan@gmail.com', N'An Giang');

INSERT INTO dbo.Office (OfficeCode, RoomNumber, Name, AreaSqm, Capacity, Location, MonthlyRent, Status, Description) VALUES
(N'OF-001', N'101', N'Office A', 50.00,  8,  N'Floor 1', 10000000.00, N'Available', N'Street-facing view'),
(N'OF-002', N'201', N'Office B', 80.00, 15,  N'Floor 2', 15000000.00, N'Available', N'Air-conditioned'),
(N'OF-003', N'301', N'Office C', 120.00, 25, N'Floor 3', 20000000.00, N'Available', N'VIP room');

INSERT INTO dbo.ServiceType (Name, Unit, DefaultUnitPrice, IsMetered) VALUES
(N'Electricity', N'kWh',   3500.00,   1),
(N'Water',       N'm3',    12000.00,  1),
(N'Internet',    N'Month', 500000.00, 0);

INSERT INTO dbo.OfficeService (OfficeId, ServiceTypeId, UnitPrice) VALUES
(1, 1, 3500.00),
(1, 2, 12000.00),
(2, 3, 500000.00),
(3, 1, 3500.00);

INSERT INTO dbo.Contract
    (ContractCode, SignedDate, StartDate, EndDate, DepositAmount, MonthlyRent, Terms, Status, TenantId, OfficeId, CreatedByEmployeeId)
VALUES
(N'CT-001', '2025-01-01', '2025-01-01', '2025-12-31', 5000000.00, 10000000.00, N'Rent due before the 5th of each month.', N'Active', 1, 1, 1),
(N'CT-002', '2025-02-01', '2025-02-01', '2026-01-31', 7000000.00, 15000000.00, N'Tenant must not modify office structure without approval.', N'Active', 2, 2, 1),
(N'CT-003', '2025-03-01', '2025-03-01', '2026-02-28', 8000000.00, 20000000.00, N'Tenant is responsible for office asset care.', N'Active', 3, 3, 2);

INSERT INTO dbo.Invoice
    (InvoiceCode, ContractId, BillingMonth, BillingYear, IssueDate, RentAmount, ServicesSubtotal, TotalAmount, Status)
VALUES
(N'INV-001', 1, 5, 2025, '2025-05-31', 10000000.00, 0, 10000000.00, N'Unpaid'),
(N'INV-002', 2, 5, 2025, '2025-05-31', 15000000.00, 0, 15000000.00, N'Unpaid'),
(N'INV-003', 3, 5, 2025, '2025-05-31', 20000000.00, 0, 20000000.00, N'Unpaid');

INSERT INTO dbo.InvoiceDetail (InvoiceId, OfficeServiceId, PreviousReading, CurrentReading, UnitPrice) VALUES
(1, 1, 100, 150, 3500.00),
(1, 2, 10,  15,  12000.00),
(2, 3, 0,   1,   500000.00),
(3, 4, 200, 260, 3500.00);

INSERT INTO dbo.MaintenanceRequest
    (RequestCode, OfficeId, TenantId, AssignedEmployeeId, Description, Priority, Status, CreatedDate)
VALUES
(N'MR-001', 1, 1, 1, N'Air conditioner not working', N'High',   N'InProgress', '2025-05-10'),
(N'MR-002', 2, 2, 2, N'Broken ceiling light',          N'Normal', N'Completed',  '2025-05-15'),
(N'MR-003', 3, 3, 1, N'Water leak in restroom',        N'Urgent', N'Pending',    '2025-05-20');

GO

/* ---------------------------------------------------------------------------
   VERIFICATION QUERIES (optional)
   --------------------------------------------------------------------------- */

-- SELECT o.Name, o.Status, c.ContractCode, t.CompanyName
-- FROM dbo.Office o
-- LEFT JOIN dbo.Contract c ON c.OfficeId = o.Id AND c.Status = N'Active'
-- LEFT JOIN dbo.Tenant t ON t.Id = c.TenantId;

-- SELECT i.InvoiceCode, i.TotalAmount, i.ServicesSubtotal, SUM(d.LineTotal) AS CalculatedServices
-- FROM dbo.Invoice i
-- LEFT JOIN dbo.InvoiceDetail d ON d.InvoiceId = i.Id
-- GROUP BY i.InvoiceCode, i.TotalAmount, i.ServicesSubtotal;
