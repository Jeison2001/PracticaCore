# Mapeo del Modelo de Datos – Contexto de Negocio (Versión Mejorada)

Este documento describe las entidades principales del esquema de base de datos, su propósito en el negocio académico y las relaciones clave entre ellas, todo en el marco del Acuerdo No. 015 del 28 de abril de 2021 de la Universidad Popular del Cesar.

## Entidades Principales

| Tabla                  | Descripción Negocio                                                                                   | Relaciones Clave / Observaciones                                                                                   |
|------------------------|------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------|
| AcademicPeriod         | Define los períodos académicos (semestres) en los que se organizan las actividades de la universidad. | Se relaciona con InscriptionModality para registrar el semestre de inicio de una modalidad.                       |
| AcademicPractice       | Gestiona las instancias de Práctica Académica como modalidad de grado, incluyendo detalles como la institución, las fechas y las horas. | Es una extensión de InscriptionModality y se vincula a los estados definidos en StateStage.                      |
| AcademicProgram        | Almacena los programas académicos ofrecidos por la universidad, como Ingeniería de Sistemas.          | Cada User (estudiante o docente) pertenece a un programa académico.                                               |
| Document               | Contiene los metadatos de los archivos cargados al sistema, como entregables, actas y formatos.       | Se asocia a una InscriptionModality y se clasifica según DocumentType. Permite el versionado de documentos.      |
| DocumentClass          | Proporciona una clasificación general para los documentos, agrupándolos por su naturaleza, como "Entregable del Estudiante" o "Acta Oficial". | Cada DocumentType pertenece a una DocumentClass.                                                                  |
| DocumentType           | Define los tipos específicos de documentos que se manejan en los procesos, como "Protocolo de Anteproyecto" o "Carta de Aceptación de la Empresa". | Se asocia a una fase (StageModality) y a una clase (DocumentClass).                                               |
| Evaluation             | Tabla genérica que registra el historial de todas las evaluaciones, revisiones y conceptos emitidos por los evaluadores sobre cualquier entidad del sistema (propuestas, informes, etc.). | Se vincula de forma polimórfica a otras tablas como Proposal o ProjectFinal y utiliza EvaluationType para especificar el tipo de revisión. |
| EvaluationType         | Cataloga los diferentes tipos de evaluaciones posibles, como "Revisión de Propuesta" o "Evaluación de Sustentación". | Se utiliza en la tabla Evaluation para dar contexto a cada registro de evaluación.                                |
| Faculty                | Representa las facultades de la universidad, como la Facultad de Ingenierías y Tecnológicas.          | Un AcademicProgram pertenece a una Faculty.                                                                       |
| IdentificationType     | Almacena los tipos de documento de identidad, como "Cédula de Ciudadanía" o "Cédula de Extranjería". | Es un campo obligatorio en la tabla User.                                                                         |
| InscriptionModality    | Tabla central que registra la inscripción de un estudiante a una modalidad de grado en un período académico específico. | Contiene el estado de la inscripción (StateInscription) y la fase actual del proceso (StageModality).             |
| Modality               | Define las 7 modalidades de grado permitidas por el Acuerdo 015, como Proyecto de Grado, Práctica Académica o Grado por Promedio. | Cada InscriptionModality se asocia a una de estas modalidades.                                                    |
| Permission             | Define los permisos granulares del sistema de forma jerárquica para controlar el acceso a funcionalidades específicas. | Se asignan a los Role mediante la tabla RolePermission.                                                           |
| PreliminaryProject     | Gestiona la fase de anteproyecto para la modalidad de Proyecto de Grado, registrando su estado y fecha de aprobación. | Es una extensión de InscriptionModality y se asocia a los estados de la fase "PG_FASE_ANTEPROYECTO".            |
| ProjectFinal           | Gestiona la fase de informe final para la modalidad de Proyecto de Grado.                            | Es una extensión de InscriptionModality y se asocia a los estados de la fase "PG_FASE_PROYECTO_INFORME".        |
| ProjectPresentation    | Registra los detalles de los eventos de sustentación de los Proyectos de Grado, incluyendo fecha, lugar, jurados y resultado final. | Se vincula a una InscriptionModality de tipo Proyecto de Grado.                                                   |
| Proposal               | Almacena los datos de la propuesta o idea inicial de un Proyecto de Grado, incluyendo título, objetivos y línea de investigación. | Es el primer paso formal en la modalidad de Proyecto de Grado y se asocia a los estados de la fase "PG_FASE_PROPUESTA". |
| ResearchLine           | Define las líneas de investigación principales a las que se adscriben los proyectos, como "TECNOLOGÍAS DE LA INFORMACIÓN". | Contiene ResearchSubLine.                                                                                         |
| ResearchSubLine        | Define las sub-líneas dentro de una línea de investigación, como "Ingeniería de Software" o "Inteligencia Artificial". | Contiene ThematicArea.                                                                                            |
| ThematicArea           | Define las áreas temáticas específicas dentro de una sub-línea de investigación, como "Arquitectura Empresarial" o "Machine Learning". | Es el nivel más granular de clasificación para la investigación.                                                   |
| Role                   | Define los roles de los usuarios en el sistema, como Estudiante, Docente, Director de comité o Jefe de departamento. | Cada User puede tener uno o más roles asignados a través de UserRole.                                             |
| StageModality          | Define las fases secuenciales que componen una modalidad de grado. Por ejemplo, para "Proyecto de Grado" las fases son: Propuesta, Anteproyecto, Informe Final y Sustentación. | Cada fase tiene un orden y agrupa un conjunto de estados (StateStage).                                            |
| StateInscription       | Define los posibles estados de una inscripción inicial a una modalidad, como "Pendiente", "Aprobado" o "Rechazado". | Un EventHandler en C# asigna el estado inicial basándose en si la modalidad requiere aprobación. |
| StateStage             | Tabla clave que define los estados específicos por los que pasa un proceso dentro de cada fase (StageModality), como "Radicada", "En Evaluación" o "Aprobado". | Es fundamental para la lógica de negocio, ya que los cambios de estado disparan transiciones automáticas mediante Domain Events. |
| TeachingAssignment     | Registra la asignación de docentes a un trabajo de grado con un rol específico, como Director, Co-Director o Jurado Evaluador. | Vincula un User (docente) a una InscriptionModality con un TypeTeachingAssignment.                                |
| TypeTeachingAssignment | Cataloga los roles que un docente puede desempeñar en una asignación académica.                     | Proporciona los tipos para TeachingAssignment.                                                                    |
| User                   | Almacena la información de todos los usuarios del sistema (estudiantes, docentes, administrativos). | Es la entidad central de actores, vinculada a roles, permisos, inscripciones y asignaciones.                      |
| EmailNotificationConfig y EmailRecipientRule | Sistema para el envío de notificaciones automáticas por correo electrónico basadas en eventos del sistema. | Las plantillas (EmailNotificationConfig) y reglas permiten una comunicación fluida. |

---

## Observaciones Generales y Flujo del Proceso 🔎

El modelo de datos está diseñado para dar soporte completo al Acuerdo 015, reflejando cada una de las modalidades y sus flujos específicos.

La arquitectura es altamente modular y escalable, utilizando tablas de estado (StateStage) y fases (StageModality) para gestionar la lógica de negocio de manera centralizada.

La automatización de procesos clave se implementa a nivel de código mediante Patrón Observer (Domain Events con MediatR). Esto incluye:

- Asignar el estado inicial a una inscripción.
- Asignar permisos a los estudiantes a medida que avanzan de fase.
- Crear instancias de AcademicPractice o Proposal cuando una inscripción es aprobada.
- Transitar automáticamente de una fase a la siguiente cuando se alcanza un estado final (ej. AP_APROBADO en Anteproyecto inicia la fase de Proyecto).

La gestión de roles y permisos es granular, permitiendo un control de acceso preciso para cada tipo de usuario, desde el estudiante hasta el administrador del sistema.

---

## Ejemplo de Flujo: Modalidad "Proyecto de Grado" 🎓

1. **Inscripción:** Un User (estudiante) con el rol "STUDENT" se inscribe a la Modality "PROYECTO_GRADO" a través de InscriptionModality. El sistema evalúa si requiere aprobación y le asigna un StateInscription ("PENDIENTE").
2. **Aprobación y Fase de Propuesta:** El comité aprueba la inscripción. El StateInscription cambia a "APROBADO" disparando el evento de dominio `InscriptionStateChangedEvent`. Los handlers actualizan la InscriptionModality a la primera StageModality ("PG_FASE_PROPUESTA") y asignan los permisos.
3. **Radicación de Propuesta:** El estudiante registra su Proposal. Esta se crea con un StateStage inicial ("PROP_RADICADA").
4. **Evaluación y Fase de Anteproyecto:** La propuesta es evaluada y su estado cambia a "PROP_PERTINENTE" disparando el evento de dominio `ProposalStateChangedEvent` que:
   - Actualiza la InscriptionModality a la siguiente fase: "PG_FASE_ANTEPROYECTO".
   - Crea una instancia en la tabla PreliminaryProject con su estado inicial.
   - Asigna al estudiante los permisos para cargar el documento del anteproyecto.
5. **Fases Subsiguientes:** El ciclo se repite. La aprobación del PreliminaryProject ("AP_APROBADO") inicia la fase de ProjectFinal ("PG_FASE_PROYECTO_INFORME"), y la aprobación de este último ("PFINF_INFORME_APROBADO") da paso a la fase de ProjectPresentation ("PG_FASE_SUSTENTACION").
6. **Notificaciones:** Durante todo el proceso, el sistema envía correos automáticos (EmailNotificationConfig) al comité, a los docentes y a los estudiantes para informar sobre cada hito relevante.

Este flujo, gobernado por la lógica en las tablas de estado y los Domain Events, asegura la correcta aplicación del Acuerdo 015 de manera automatizada y trazable.
