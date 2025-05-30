# CrowdQR Architecture Documentation

## System Architecture Overview

![System Architecture Diagram](SystemArchitectureOverview.png)

## User Workflow

### DJ Workflow

DJ Registration & Setup:

![DJ Registration & Setup Diagram](DJRegistrationWorkflow.png)

Live Event Management:

![Live Event Management Diagram](LiveEventManagement.png)

### Audience Workflow

Audience Participation:

![Audience Participation Diagram](AudienceParticipation.png)

Real-time Experience:

![Real-time Experience Diagram](RealTimeExperience.png)

## Data Flow Architecture

Audience Request Submission:

![Audience Request Submission Diagram](DataFlow_AudienceRequest.png)

DJ Approval Process:

![DJ Approval Process Diagram](DataFlow_DJApproval.png)

## Component Interaction

SignalR Real-time Communication:

![SignalR Real-time Communication Diagram](SignalR.png)

## Security Architecture

Authentication & Authorization Flow:

![Authentication & Authorization Flow Diagram](Authentication_Authorization.png)

## Database Schema Overview

![Database Schema Overview Diagram](erd/erd.png)

## Deployment Architecture

Container Deployment:

![Container Deployment Diagram](ContainerDeployment.png)

For implementation details, see:

- [API Documentation](../src/CrowdQR.Api/)
- [Web Application](../src/CrowdQR.Web/)  
- [Deployment Guide](../deploy/README.md)