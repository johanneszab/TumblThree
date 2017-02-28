namespace RateLimiter
{
    public static class IAwaitableConstraintExtension
    {
        public static IAwaitableConstraint Compose(this IAwaitableConstraint awaitableConstraint1, IAwaitableConstraint awaitableConstraint2)
        {
            if (awaitableConstraint1 == awaitableConstraint2)
                return awaitableConstraint1;

            return new ComposedAwaitableConstraint(awaitableConstraint1, awaitableConstraint2);
        }
    }
}
