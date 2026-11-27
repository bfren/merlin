// Merlin: .NET HTTP Server.
// Copyright (c) bfren - licensed under https://mit.bfren.dev/2022

using Microsoft.AspNetCore.Http;
using Wrap;

namespace Merlin.Domain;

/// <summary>
/// An HTTP response status code, e.g. <c>200</c>.
/// </summary>
public sealed record class Status : Monad<Status, int>
{
	/// <summary>
	/// <c>100 Continue</c> status.
	/// </summary>
	public static readonly Status Continue =
		Wrap(StatusCodes.Status100Continue);

	/// <summary>
	/// <c>101 Switching Protocols</c> status.
	/// </summary>
	public static readonly Status SwitchingProtocols =
		Wrap(StatusCodes.Status101SwitchingProtocols);

	/// <summary>
	/// <c>102 Processing</c> status.
	/// </summary>
	public static readonly Status Processing =
		Wrap(StatusCodes.Status102Processing);

	/// <summary>
	/// <c>200 OK</c> status.
	/// </summary>
	public static readonly Status Ok =
		Wrap(StatusCodes.Status200OK);

	/// <summary>
	/// <c>201 Created</c> status.
	/// </summary>
	public static readonly Status Created =
		Wrap(StatusCodes.Status201Created);

	/// <summary>
	/// <c>202 Accepted</c> status.
	/// </summary>
	public static readonly Status Accepted =
		Wrap(StatusCodes.Status202Accepted);

	/// <summary>
	/// <c>203 Non-Authoritative Information</c> status.
	/// </summary>
	public static readonly Status NonAuthoritative =
		Wrap(StatusCodes.Status203NonAuthoritative);

	/// <summary>
	/// <c>204 No Content</c> status.
	/// </summary>
	public static readonly Status NoContent =
		Wrap(StatusCodes.Status204NoContent);

	/// <summary>
	/// <c>205 Reset Content</c> status.
	/// </summary>
	public static readonly Status ResetContent =
		Wrap(StatusCodes.Status205ResetContent);

	/// <summary>
	/// <c>206 Partial Content</c> status.
	/// </summary>
	public static readonly Status PartialContent =
		Wrap(StatusCodes.Status206PartialContent);

	/// <summary>
	/// <c>207 Multi-Status</c> status.
	/// </summary>
	public static readonly Status MultiStatus =
		Wrap(StatusCodes.Status207MultiStatus);

	/// <summary>
	/// <c>208 Already Reported</c> status.
	/// </summary>
	public static readonly Status AlreadyReported =
		Wrap(StatusCodes.Status208AlreadyReported);

	/// <summary>
	/// <c>226 IM Used</c> status.
	/// </summary>
	public static readonly Status IMUsed =
		Wrap(StatusCodes.Status226IMUsed);

	/// <summary>
	/// <c>300 Multiple Choices</c> status.
	/// </summary>
	public static readonly Status MultipleChoices =
		Wrap(StatusCodes.Status300MultipleChoices);

	/// <summary>
	/// <c>301 Moved Permanently</c> status.
	/// </summary>
	public static readonly Status MovedPermanently =
		Wrap(StatusCodes.Status301MovedPermanently);

	/// <summary>
	/// <c>302 Found</c> status.
	/// </summary>
	public static readonly Status Found =
		Wrap(StatusCodes.Status302Found);

	/// <summary>
	/// <c>303 See Other</c> status.
	/// </summary>
	public static readonly Status SeeOther =
		Wrap(StatusCodes.Status303SeeOther);

	/// <summary>
	/// <c>304 Not Modified</c> status.
	/// </summary>
	public static readonly Status NotModified =
		Wrap(StatusCodes.Status304NotModified);

	/// <summary>
	/// <c>305 Use Proxy</c> status.
	/// </summary>
	public static readonly Status UseProxy =
		Wrap(StatusCodes.Status305UseProxy);

	/// <summary>
	/// <c>306 Switch Proxy</c> status.
	/// </summary>
	public static readonly Status SwitchProxy =
		Wrap(StatusCodes.Status306SwitchProxy);

	/// <summary>
	/// <c>307 Temporary Redirect</c> status.
	/// </summary>
	public static readonly Status TemporaryRedirect =
		Wrap(StatusCodes.Status307TemporaryRedirect);

	/// <summary>
	/// <c>308 Permanent Redirect</c> status.
	/// </summary>
	public static readonly Status PermanentRedirect =
		Wrap(StatusCodes.Status308PermanentRedirect);

	/// <summary>
	/// <c>400 Bad Request</c> status.
	/// </summary>
	public static readonly Status BadRequest =
		Wrap(StatusCodes.Status400BadRequest);

	/// <summary>
	/// <c>401 Unauthorized</c> status.
	/// </summary>
	public static readonly Status Unauthorized =
		Wrap(StatusCodes.Status401Unauthorized);

	/// <summary>
	/// <c>402 Payment Required</c> status.
	/// </summary>
	public static readonly Status PaymentRequired =
		Wrap(StatusCodes.Status402PaymentRequired);

	/// <summary>
	/// <c>403 Forbidden</c> status.
	/// </summary>
	public static readonly Status Forbidden =
		Wrap(StatusCodes.Status403Forbidden);

	/// <summary>
	/// <c>404 Not Found</c> status.
	/// </summary>
	public static readonly Status NotFound =
		Wrap(StatusCodes.Status404NotFound);

	/// <summary>
	/// <c>405 Method Not Allowed</c> status.
	/// </summary>
	public static readonly Status MethodNotAllowed =
		Wrap(StatusCodes.Status405MethodNotAllowed);

	/// <summary>
	/// <c>406 Not Acceptable</c> status.
	/// </summary>
	public static readonly Status NotAcceptable =
		Wrap(StatusCodes.Status406NotAcceptable);

	/// <summary>
	/// <c>407 Proxy Authentication Required</c> status.
	/// </summary>
	public static readonly Status ProxyAuthenticationRequired =
		Wrap(StatusCodes.Status407ProxyAuthenticationRequired);

	/// <summary>
	/// <c>408 Request Timeout</c> status.
	/// </summary>
	public static readonly Status RequestTimeout =
		Wrap(StatusCodes.Status408RequestTimeout);

	/// <summary>
	/// <c>409 Conflict</c> status.
	/// </summary>
	public static readonly Status Conflict =
		Wrap(StatusCodes.Status409Conflict);

	/// <summary>
	/// <c>410 Gone</c> status.
	/// </summary>
	public static readonly Status Gone =
		Wrap(StatusCodes.Status410Gone);

	/// <summary>
	/// <c>411 Length Required</c> status.
	/// </summary>
	public static readonly Status LengthRequired =
		Wrap(StatusCodes.Status411LengthRequired);

	/// <summary>
	/// <c>412 Precondition Failed</c> status.
	/// </summary>
	public static readonly Status PreconditionFailed =
		Wrap(StatusCodes.Status412PreconditionFailed);

	/// <summary>
	/// <c>413 Payload Too Large</c> status.
	/// </summary>
	public static readonly Status PayloadTooLarge =
		Wrap(StatusCodes.Status413PayloadTooLarge);

	/// <summary>
	/// <c>414 URI Too Long</c> status.
	/// </summary>
	public static readonly Status UriTooLong =
		Wrap(StatusCodes.Status414UriTooLong);

	/// <summary>
	/// <c>415 Unsupported Media Type</c> status.
	/// </summary>
	public static readonly Status UnsupportedMediaType =
		Wrap(StatusCodes.Status415UnsupportedMediaType);

	/// <summary>
	/// <c>416 Range Not Satisfiable</c> status.
	/// </summary>
	public static readonly Status RangeNotSatisfiable =
		Wrap(StatusCodes.Status416RangeNotSatisfiable);

	/// <summary>
	/// <c>417 Expectation Failed</c> status.
	/// </summary>
	public static readonly Status ExpectationFailed =
		Wrap(StatusCodes.Status417ExpectationFailed);

	/// <summary>
	/// <c>418 I'm a teapot</c> status.
	/// </summary>
	public static readonly Status ImATeapot =
		Wrap(StatusCodes.Status418ImATeapot);

	/// <summary>
	/// <c>419 Authentication Timeout</c> status.
	/// </summary>
	public static readonly Status AuthenticationTimeout =
		Wrap(StatusCodes.Status419AuthenticationTimeout);

	/// <summary>
	/// <c>421 Misdirected Request</c> status.
	/// </summary>
	public static readonly Status MisdirectedRequest =
		Wrap(StatusCodes.Status421MisdirectedRequest);

	/// <summary>
	/// <c>422 Unprocessable Entity</c> status.
	/// </summary>
	public static readonly Status UnprocessableEntity =
		Wrap(StatusCodes.Status422UnprocessableEntity);

	/// <summary>
	/// <c>423 Locked</c> status.
	/// </summary>
	public static readonly Status Locked =
		Wrap(StatusCodes.Status423Locked);

	/// <summary>
	/// <c>424 Failed Dependency</c> status.
	/// </summary>
	public static readonly Status FailedDependency =
		Wrap(StatusCodes.Status424FailedDependency);

	/// <summary>
	/// <c>426 Upgrade Required</c> status.
	/// </summary>
	public static readonly Status UpgradeRequired =
		Wrap(StatusCodes.Status426UpgradeRequired);

	/// <summary>
	/// <c>428 Precondition Required</c> status.
	/// </summary>
	public static readonly Status PreconditionRequired =
		Wrap(StatusCodes.Status428PreconditionRequired);

	/// <summary>
	/// <c>429 Too Many Requests</c> status.
	/// </summary>
	public static readonly Status TooManyRequests =
		Wrap(StatusCodes.Status429TooManyRequests);

	/// <summary>
	/// <c>431 Request Header Fields Too Large</c> status.
	/// </summary>
	public static readonly Status RequestHeaderFieldsTooLarge =
		Wrap(StatusCodes.Status431RequestHeaderFieldsTooLarge);

	/// <summary>
	/// <c>451 Unavailable For Legal Reasons</c> status.
	/// </summary>
	public static readonly Status UnavailableForLegalReasons =
		Wrap(StatusCodes.Status451UnavailableForLegalReasons);

	/// <summary>
	/// <c>499 Client Closed Request</c> status.
	/// </summary>
	public static readonly Status ClientClosedRequest =
		Wrap(StatusCodes.Status499ClientClosedRequest);

	/// <summary>
	/// <c>500 Internal Server Error</c> status.
	/// </summary>
	public static readonly Status InternalServerError =
		Wrap(StatusCodes.Status500InternalServerError);

	/// <summary>
	/// <c>501 Not Implemented</c> status.
	/// </summary>
	public static readonly Status NotImplemented =
		Wrap(StatusCodes.Status501NotImplemented);

	/// <summary>
	/// <c>502 Bad Gateway</c> status.
	/// </summary>
	public static readonly Status BadGateway =
		Wrap(StatusCodes.Status502BadGateway);

	/// <summary>
	/// <c>503 Service Unavailable</c> status.
	/// </summary>
	public static readonly Status ServiceUnavailable =
		Wrap(StatusCodes.Status503ServiceUnavailable);

	/// <summary>
	/// <c>504 Gateway Timeout</c> status.
	/// </summary>
	public static readonly Status GatewayTimeout =
		Wrap(StatusCodes.Status504GatewayTimeout);

	/// <summary>
	/// <c>505 HTTP Version Not Supported</c> status.
	/// </summary>
	public static readonly Status HttpVersionNotSupported =
		Wrap(StatusCodes.Status505HttpVersionNotsupported);

	/// <summary>
	/// <c>506 Variant Also Negotiates</c> status.
	/// </summary>
	public static readonly Status VariantAlsoNegotiates =
		Wrap(StatusCodes.Status506VariantAlsoNegotiates);

	/// <summary>
	/// <c>507 Insufficient Storage</c> status.
	/// </summary>
	public static readonly Status InsufficientStorage =
		Wrap(StatusCodes.Status507InsufficientStorage);

	/// <summary>
	/// <c>508 Loop Detected</c> status.
	/// </summary>
	public static readonly Status LoopDetected =
		Wrap(StatusCodes.Status508LoopDetected);

	/// <summary>
	/// <c>510 Not Extended</c> status.
	/// </summary>
	public static readonly Status NotExtended =
		Wrap(StatusCodes.Status510NotExtended);

	/// <summary>
	/// <c>511 Network Authentication Required</c> status.
	/// </summary>
	public static readonly Status NetworkAuthenticationRequired =
		Wrap(StatusCodes.Status511NetworkAuthenticationRequired);
}
