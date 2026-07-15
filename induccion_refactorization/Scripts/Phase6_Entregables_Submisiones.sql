/****** Phase 6: Student Task/File Submission System ******/
/****** Object: Table [dbo].[Ind_Entregables] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Ind_Entregables](
	[EntregableID] [int] IDENTITY(1,1) NOT NULL,
	[UnidadID] [int] NOT NULL,
	[Titulo] [nvarchar](255) NOT NULL,
	[Instrucciones] [nvarchar](max) NULL,
	[FechaLimite] [datetime] NULL,
	[PonderacionMax] [decimal](5, 2) NOT NULL,
	[Activo] [bit] NOT NULL,
 CONSTRAINT [PK_IndEntregables] PRIMARY KEY CLUSTERED
(
	[EntregableID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object: Table [dbo].[Ind_Submisiones] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Ind_Submisiones](
	[SubmisionID] [int] IDENTITY(1,1) NOT NULL,
	[AspiranteID] [int] NOT NULL,
	[EntregableID] [int] NOT NULL,
	[RutaArchivo] [nvarchar](500) NOT NULL,
	[FechaEnvio] [datetime] NOT NULL,
	[Estado] [nvarchar](50) NOT NULL,
	[Calificacion] [decimal](5, 2) NULL,
	[ComentarioRevisor] [nvarchar](max) NULL,
	[UsuarioRevisorID] [int] NULL,
	[FechaRevision] [datetime] NULL,
 CONSTRAINT [PK_IndSubmisiones] PRIMARY KEY CLUSTERED
(
	[SubmisionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Defaults ******/
ALTER TABLE [dbo].[Ind_Entregables] ADD  CONSTRAINT [DF_IndEntregables_Activo]  DEFAULT ((1)) FOR [Activo]
GO
ALTER TABLE [dbo].[Ind_Entregables] ADD  CONSTRAINT [DF_IndEntregables_PonderacionMax]  DEFAULT ((100)) FOR [PonderacionMax]
GO
ALTER TABLE [dbo].[Ind_Submisiones] ADD  CONSTRAINT [DF_IndSubmisiones_FechaEnvio]  DEFAULT (getdate()) FOR [FechaEnvio]
GO
ALTER TABLE [dbo].[Ind_Submisiones] ADD  CONSTRAINT [DF_IndSubmisiones_Estado]  DEFAULT ('Pendiente') FOR [Estado]
GO
/****** Foreign Keys ******/
ALTER TABLE [dbo].[Ind_Entregables]  WITH CHECK ADD  CONSTRAINT [FK_IndEntregables_Unidades] FOREIGN KEY([UnidadID])
REFERENCES [dbo].[Ind_Unidades] ([UnidadID])
GO
ALTER TABLE [dbo].[Ind_Entregables] CHECK CONSTRAINT [FK_IndEntregables_Unidades]
GO
ALTER TABLE [dbo].[Ind_Submisiones]  WITH CHECK ADD  CONSTRAINT [FK_IndSubmisiones_Aspirantes] FOREIGN KEY([AspiranteID])
REFERENCES [dbo].[Aspirantes] ([AspiranteID])
GO
ALTER TABLE [dbo].[Ind_Submisiones] CHECK CONSTRAINT [FK_IndSubmisiones_Aspirantes]
GO
ALTER TABLE [dbo].[Ind_Submisiones]  WITH CHECK ADD  CONSTRAINT [FK_IndSubmisiones_Entregables] FOREIGN KEY([EntregableID])
REFERENCES [dbo].[Ind_Entregables] ([EntregableID])
GO
ALTER TABLE [dbo].[Ind_Submisiones] CHECK CONSTRAINT [FK_IndSubmisiones_Entregables]
GO
ALTER TABLE [dbo].[Ind_Submisiones]  WITH CHECK ADD  CONSTRAINT [FK_IndSubmisiones_Usuarios] FOREIGN KEY([UsuarioRevisorID])
REFERENCES [dbo].[Usuarios] ([UsuarioID])
GO
ALTER TABLE [dbo].[Ind_Submisiones] CHECK CONSTRAINT [FK_IndSubmisiones_Usuarios]
GO
/****** Estado CHECK constraint: 1-step grading workflow (Pendiente -> Revisado | Rechazado) ******/
ALTER TABLE [dbo].[Ind_Submisiones]  WITH CHECK ADD  CONSTRAINT [CK_IndSubmisiones_Estado] CHECK
(([Estado]='Rechazado' OR [Estado]='Revisado' OR [Estado]='Pendiente'))
GO
ALTER TABLE [dbo].[Ind_Submisiones] CHECK CONSTRAINT [CK_IndSubmisiones_Estado]
GO
