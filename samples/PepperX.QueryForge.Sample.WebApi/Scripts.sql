SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TestUsers](
	[UserId] [int] IDENTITY(1,1) NOT NULL,
	[FirstName] [nvarchar](50) NOT NULL,
	[LastName] [nvarchar](50) NOT NULL,
	[Email] [nvarchar](100) NOT NULL,
	[Country] [nvarchar](50) NOT NULL,
	[City] [nvarchar](50) NOT NULL,
	[Department] [nvarchar](50) NOT NULL,
	[Age] [int] NOT NULL,
	[Score] [decimal](5, 2) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedOn] [datetime2](7) NOT NULL,
	[DeletedAt] [datetime2](7) NULL,
	[Role] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[TestUsers] ON 
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (1, N'First1', N'Last1', N'user1@test.com', N'Germany', N'Munich', N'HR', 19, CAST(51.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-24T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (2, N'First2', N'Last2', N'user2@test.com', N'Japan', N'Tokyo', N'Sales', 20, CAST(52.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-23T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (3, N'First3', N'Last3', N'user3@test.com', N'UK', N'Manchester', N'Marketing', 21, CAST(53.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-22T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (4, N'First4', N'Last4', N'user4@test.com', N'Canada', N'Toronto', N'IT', 22, CAST(54.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-21T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (5, N'First5', N'Last5', N'user5@test.com', N'USA', N'Chicago', N'HR', 23, CAST(55.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-20T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (6, N'First6', N'Last6', N'user6@test.com', N'Germany', N'Berlin', N'Sales', 24, CAST(56.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-19T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (7, N'First7', N'Last7', N'user7@test.com', N'Japan', N'Osaka', N'Marketing', 25, CAST(57.00 AS Decimal(5, 2)), 0, CAST(N'2026-06-18T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (8, N'First8', N'Last8', N'user8@test.com', N'UK', N'London', N'IT', 26, CAST(58.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-17T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (9, N'First9', N'Last9', N'user9@test.com', N'Canada', N'Vancouver', N'HR', 27, CAST(59.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-16T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (10, N'First10', N'Last10', N'user10@test.com', N'USA', N'Los Angeles', N'Sales', 28, CAST(60.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-15T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (11, N'First11', N'Last11', N'user11@test.com', N'Germany', N'Munich', N'Marketing', 29, CAST(61.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-14T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (12, N'First12', N'Last12', N'user12@test.com', N'Japan', N'Tokyo', N'IT', 30, CAST(62.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-13T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (13, N'First13', N'Last13', N'user13@test.com', N'UK', N'Manchester', N'HR', 31, CAST(63.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-12T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (14, N'First14', N'Last14', N'user14@test.com', N'Canada', N'Toronto', N'Sales', 32, CAST(64.00 AS Decimal(5, 2)), 0, CAST(N'2026-06-11T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (15, N'First15', N'Last15', N'user15@test.com', N'USA', N'New York', N'Marketing', 33, CAST(65.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-10T01:24:22.7674323' AS DateTime2), CAST(N'2026-06-11T01:24:22.7674323' AS DateTime2), N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (16, N'First16', N'Last16', N'user16@test.com', N'Germany', N'Berlin', N'IT', 34, CAST(66.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-09T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (17, N'First17', N'Last17', N'user17@test.com', N'Japan', N'Osaka', N'HR', 35, CAST(67.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-08T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (18, N'First18', N'Last18', N'user18@test.com', N'UK', N'London', N'Sales', 36, CAST(68.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-07T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (19, N'First19', N'Last19', N'user19@test.com', N'Canada', N'Vancouver', N'Marketing', 37, CAST(69.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-06T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (20, N'First20', N'Last20', N'user20@test.com', N'USA', N'Chicago', N'IT', 38, CAST(70.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-05T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (21, N'First21', N'Last21', N'user21@test.com', N'Germany', N'Munich', N'HR', 39, CAST(71.00 AS Decimal(5, 2)), 0, CAST(N'2026-06-04T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (22, N'First22', N'Last22', N'user22@test.com', N'Japan', N'Tokyo', N'Sales', 40, CAST(72.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-03T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (23, N'First23', N'Last23', N'user23@test.com', N'UK', N'Manchester', N'Marketing', 41, CAST(73.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-02T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (24, N'First24', N'Last24', N'user24@test.com', N'Canada', N'Toronto', N'IT', 42, CAST(74.00 AS Decimal(5, 2)), 1, CAST(N'2026-06-01T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (25, N'First25', N'Last25', N'user25@test.com', N'USA', N'Los Angeles', N'HR', 43, CAST(75.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-31T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (26, N'First26', N'Last26', N'user26@test.com', N'Germany', N'Berlin', N'Sales', 44, CAST(76.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-30T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (27, N'First27', N'Last27', N'user27@test.com', N'Japan', N'Osaka', N'Marketing', 45, CAST(77.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-29T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (28, N'First28', N'Last28', N'user28@test.com', N'UK', N'London', N'IT', 46, CAST(78.00 AS Decimal(5, 2)), 0, CAST(N'2026-05-28T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (29, N'First29', N'Last29', N'user29@test.com', N'Canada', N'Vancouver', N'HR', 47, CAST(79.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-27T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (30, N'First30', N'Last30', N'user30@test.com', N'USA', N'New York', N'Sales', 48, CAST(80.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-26T01:24:22.7674323' AS DateTime2), CAST(N'2026-05-27T01:24:22.7674323' AS DateTime2), N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (31, N'First31', N'Last31', N'user31@test.com', N'Germany', N'Munich', N'Marketing', 49, CAST(81.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-25T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (32, N'First32', N'Last32', N'user32@test.com', N'Japan', N'Tokyo', N'IT', 50, CAST(82.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-24T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (33, N'First33', N'Last33', N'user33@test.com', N'UK', N'Manchester', N'HR', 51, CAST(83.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-23T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (34, N'First34', N'Last34', N'user34@test.com', N'Canada', N'Toronto', N'Sales', 52, CAST(84.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-22T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (35, N'First35', N'Last35', N'user35@test.com', N'USA', N'Chicago', N'Marketing', 53, CAST(85.00 AS Decimal(5, 2)), 0, CAST(N'2026-05-21T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (36, N'First36', N'Last36', N'user36@test.com', N'Germany', N'Berlin', N'IT', 54, CAST(86.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-20T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (37, N'First37', N'Last37', N'user37@test.com', N'Japan', N'Osaka', N'HR', 55, CAST(87.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-19T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (38, N'First38', N'Last38', N'user38@test.com', N'UK', N'London', N'Sales', 56, CAST(88.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-18T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (39, N'First39', N'Last39', N'user39@test.com', N'Canada', N'Vancouver', N'Marketing', 57, CAST(89.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-17T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (40, N'First40', N'Last40', N'user40@test.com', N'USA', N'Los Angeles', N'IT', 58, CAST(90.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-16T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (41, N'First41', N'Last41', N'user41@test.com', N'Germany', N'Munich', N'HR', 59, CAST(91.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-15T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (42, N'First42', N'Last42', N'user42@test.com', N'Japan', N'Tokyo', N'Sales', 60, CAST(92.00 AS Decimal(5, 2)), 0, CAST(N'2026-05-14T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (43, N'First43', N'Last43', N'user43@test.com', N'UK', N'Manchester', N'Marketing', 61, CAST(93.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-13T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (44, N'First44', N'Last44', N'user44@test.com', N'Canada', N'Toronto', N'IT', 62, CAST(94.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-12T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (45, N'First45', N'Last45', N'user45@test.com', N'USA', N'New York', N'HR', 63, CAST(95.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-11T01:24:22.7674323' AS DateTime2), CAST(N'2026-05-12T01:24:22.7674323' AS DateTime2), N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (46, N'First46', N'Last46', N'user46@test.com', N'Germany', N'Berlin', N'Sales', 64, CAST(96.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-10T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (47, N'First47', N'Last47', N'user47@test.com', N'Japan', N'Osaka', N'Marketing', 65, CAST(97.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-09T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (48, N'First48', N'Last48', N'user48@test.com', N'UK', N'London', N'IT', 66, CAST(98.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-08T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (49, N'First49', N'Last49', N'user49@test.com', N'Canada', N'Vancouver', N'HR', 67, CAST(99.00 AS Decimal(5, 2)), 0, CAST(N'2026-05-07T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (50, N'First50', N'Last50', N'user50@test.com', N'USA', N'Chicago', N'Sales', 18, CAST(50.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-06T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (51, N'First51', N'Last51', N'user51@test.com', N'Germany', N'Munich', N'Marketing', 19, CAST(51.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-05T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (52, N'First52', N'Last52', N'user52@test.com', N'Japan', N'Tokyo', N'IT', 20, CAST(52.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-04T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (53, N'First53', N'Last53', N'user53@test.com', N'UK', N'Manchester', N'HR', 21, CAST(53.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-03T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (54, N'First54', N'Last54', N'user54@test.com', N'Canada', N'Toronto', N'Sales', 22, CAST(54.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-02T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (55, N'First55', N'Last55', N'user55@test.com', N'USA', N'Los Angeles', N'Marketing', 23, CAST(55.00 AS Decimal(5, 2)), 1, CAST(N'2026-05-01T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (56, N'First56', N'Last56', N'user56@test.com', N'Germany', N'Berlin', N'IT', 24, CAST(56.00 AS Decimal(5, 2)), 0, CAST(N'2026-04-30T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (57, N'First57', N'Last57', N'user57@test.com', N'Japan', N'Osaka', N'HR', 25, CAST(57.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-29T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (58, N'First58', N'Last58', N'user58@test.com', N'UK', N'London', N'Sales', 26, CAST(58.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-28T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (59, N'First59', N'Last59', N'user59@test.com', N'Canada', N'Vancouver', N'Marketing', 27, CAST(59.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-27T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (60, N'First60', N'Last60', N'user60@test.com', N'USA', N'New York', N'IT', 28, CAST(60.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-26T01:24:22.7674323' AS DateTime2), CAST(N'2026-04-27T01:24:22.7674323' AS DateTime2), N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (61, N'First61', N'Last61', N'user61@test.com', N'Germany', N'Munich', N'HR', 29, CAST(61.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-25T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (62, N'First62', N'Last62', N'user62@test.com', N'Japan', N'Tokyo', N'Sales', 30, CAST(62.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-24T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (63, N'First63', N'Last63', N'user63@test.com', N'UK', N'Manchester', N'Marketing', 31, CAST(63.00 AS Decimal(5, 2)), 0, CAST(N'2026-04-23T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (64, N'First64', N'Last64', N'user64@test.com', N'Canada', N'Toronto', N'IT', 32, CAST(64.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-22T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (65, N'First65', N'Last65', N'user65@test.com', N'USA', N'Chicago', N'HR', 33, CAST(65.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-21T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (66, N'First66', N'Last66', N'user66@test.com', N'Germany', N'Berlin', N'Sales', 34, CAST(66.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-20T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (67, N'First67', N'Last67', N'user67@test.com', N'Japan', N'Osaka', N'Marketing', 35, CAST(67.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-19T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (68, N'First68', N'Last68', N'user68@test.com', N'UK', N'London', N'IT', 36, CAST(68.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-18T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (69, N'First69', N'Last69', N'user69@test.com', N'Canada', N'Vancouver', N'HR', 37, CAST(69.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-17T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (70, N'First70', N'Last70', N'user70@test.com', N'USA', N'Los Angeles', N'Sales', 38, CAST(70.00 AS Decimal(5, 2)), 0, CAST(N'2026-04-16T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (71, N'First71', N'Last71', N'user71@test.com', N'Germany', N'Munich', N'Marketing', 39, CAST(71.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-15T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (72, N'First72', N'Last72', N'user72@test.com', N'Japan', N'Tokyo', N'IT', 40, CAST(72.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-14T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (73, N'First73', N'Last73', N'user73@test.com', N'UK', N'Manchester', N'HR', 41, CAST(73.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-13T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (74, N'First74', N'Last74', N'user74@test.com', N'Canada', N'Toronto', N'Sales', 42, CAST(74.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-12T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (75, N'First75', N'Last75', N'user75@test.com', N'USA', N'New York', N'Marketing', 43, CAST(75.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-11T01:24:22.7674323' AS DateTime2), CAST(N'2026-04-12T01:24:22.7674323' AS DateTime2), N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (76, N'First76', N'Last76', N'user76@test.com', N'Germany', N'Berlin', N'IT', 44, CAST(76.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-10T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (77, N'First77', N'Last77', N'user77@test.com', N'Japan', N'Osaka', N'HR', 45, CAST(77.00 AS Decimal(5, 2)), 0, CAST(N'2026-04-09T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (78, N'First78', N'Last78', N'user78@test.com', N'UK', N'London', N'Sales', 46, CAST(78.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-08T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (79, N'First79', N'Last79', N'user79@test.com', N'Canada', N'Vancouver', N'Marketing', 47, CAST(79.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-07T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (80, N'First80', N'Last80', N'user80@test.com', N'USA', N'Chicago', N'IT', 48, CAST(80.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-06T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (81, N'First81', N'Last81', N'user81@test.com', N'Germany', N'Munich', N'HR', 49, CAST(81.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-05T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (82, N'First82', N'Last82', N'user82@test.com', N'Japan', N'Tokyo', N'Sales', 50, CAST(82.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-04T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (83, N'First83', N'Last83', N'user83@test.com', N'UK', N'Manchester', N'Marketing', 51, CAST(83.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-03T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (84, N'First84', N'Last84', N'user84@test.com', N'Canada', N'Toronto', N'IT', 52, CAST(84.00 AS Decimal(5, 2)), 0, CAST(N'2026-04-02T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (85, N'First85', N'Last85', N'user85@test.com', N'USA', N'Los Angeles', N'HR', 53, CAST(85.00 AS Decimal(5, 2)), 1, CAST(N'2026-04-01T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (86, N'First86', N'Last86', N'user86@test.com', N'Germany', N'Berlin', N'Sales', 54, CAST(86.00 AS Decimal(5, 2)), 1, CAST(N'2026-03-31T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (87, N'First87', N'Last87', N'user87@test.com', N'Japan', N'Osaka', N'Marketing', 55, CAST(87.00 AS Decimal(5, 2)), 1, CAST(N'2026-03-30T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (88, N'First88', N'Last88', N'user88@test.com', N'UK', N'London', N'IT', 56, CAST(88.00 AS Decimal(5, 2)), 1, CAST(N'2026-03-29T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (89, N'First89', N'Last89', N'user89@test.com', N'Canada', N'Vancouver', N'HR', 57, CAST(89.00 AS Decimal(5, 2)), 1, CAST(N'2026-03-28T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (90, N'First90', N'Last90', N'user90@test.com', N'USA', N'New York', N'Sales', 58, CAST(90.00 AS Decimal(5, 2)), 1, CAST(N'2026-03-27T01:24:22.7674323' AS DateTime2), CAST(N'2026-03-28T01:24:22.7674323' AS DateTime2), N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (91, N'First91', N'Last91', N'user91@test.com', N'Germany', N'Munich', N'Marketing', 59, CAST(91.00 AS Decimal(5, 2)), 0, CAST(N'2026-03-26T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (92, N'First92', N'Last92', N'user92@test.com', N'Japan', N'Tokyo', N'IT', 60, CAST(92.00 AS Decimal(5, 2)), 1, CAST(N'2026-03-25T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (93, N'First93', N'Last93', N'user93@test.com', N'UK', N'Manchester', N'HR', 61, CAST(93.00 AS Decimal(5, 2)), 1, CAST(N'2026-03-24T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (94, N'First94', N'Last94', N'user94@test.com', N'Canada', N'Toronto', N'Sales', 62, CAST(94.00 AS Decimal(5, 2)), 1, CAST(N'2026-03-23T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (95, N'First95', N'Last95', N'user95@test.com', N'USA', N'Chicago', N'Marketing', 63, CAST(95.00 AS Decimal(5, 2)), 1, CAST(N'2026-03-22T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (96, N'First96', N'Last96', N'user96@test.com', N'Germany', N'Berlin', N'IT', 64, CAST(96.00 AS Decimal(5, 2)), 1, CAST(N'2026-03-21T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (97, N'First97', N'Last97', N'user97@test.com', N'Japan', N'Osaka', N'HR', 65, CAST(97.00 AS Decimal(5, 2)), 1, CAST(N'2026-03-20T01:24:22.7674323' AS DateTime2), NULL, N'User')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (98, N'First98', N'Last98', N'user98@test.com', N'UK', N'London', N'Sales', 66, CAST(98.00 AS Decimal(5, 2)), 0, CAST(N'2026-03-19T01:24:22.7674323' AS DateTime2), NULL, N'Moderator')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (99, N'First99', N'Last99', N'user99@test.com', N'Canada', N'Vancouver', N'Marketing', 67, CAST(99.00 AS Decimal(5, 2)), 1, CAST(N'2026-03-18T01:24:22.7674323' AS DateTime2), NULL, N'Guest')
GO
INSERT [dbo].[TestUsers] ([UserId], [FirstName], [LastName], [Email], [Country], [City], [Department], [Age], [Score], [IsActive], [CreatedOn], [DeletedAt], [Role]) VALUES (100, N'First100', N'Last100', N'user100@test.com', N'USA', N'Los Angeles', N'IT', 18, CAST(50.00 AS Decimal(5, 2)), 1, CAST(N'2026-03-17T01:24:22.7674323' AS DateTime2), NULL, N'Admin')
GO
SET IDENTITY_INSERT [dbo].[TestUsers] OFF
GO

CREATE OR ALTER VIEW [dbo].[vw_ActiveUsers]
AS
SELECT 
    UserId, FirstName, LastName, Email, Country, City, 
    Department, Age, Score, IsActive, CreatedOn, DeletedAt, Role
FROM [dbo].[TestUsers]
WHERE IsActive = 1 AND DeletedAt IS NULL;
GO

CREATE OR ALTER FUNCTION [dbo].[tvf_GetUsersByTenant]
(
    @TenantId INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        UserId, FirstName, LastName, Email, Country, City, 
		Department, Age, Score, IsActive, CreatedOn, DeletedAt, Role
    FROM [dbo].[TestUsers]
    WHERE IsActive = 1 
      AND (@TenantId = 0 OR UserId % 5 = @TenantId) 
);
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_GetUserReport]
(
    @IncludeDeleted BIT = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        UserId, FirstName, LastName, Email, Country, City, 
        Department, Age, Score, IsActive, CreatedOn, DeletedAt, Role
    FROM [dbo].[TestUsers]
    WHERE (@IncludeDeleted = 1 OR DeletedAt IS NULL);
END;
GO