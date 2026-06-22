package demo;

/**
 * SpotBugsDemo - classroom example with intentional simple programming mistakes.
 *
 * SpotBugs analyzes compiled Java bytecode and reports these common issues.
 * This file is part of Demo 1: SpotBugs Report Generation.
 */
public class SpotBugsDemo {

    // --- Bug 1: EI_EXPOSE_REP - returning internal mutable array directly ---
    private int[] scores = {90, 85, 70, 95};

    public int[] getScores() {
        // Bug: caller can modify internal array
        return scores;
    }

    // --- Bug 2: EI_EXPOSE_REP2 - storing external mutable array directly ---
    private String[] tags;

    public void setTags(String[] tags) {
        // Bug: stores reference without defensive copy
        this.tags = tags;
    }

    // --- Bug 3: HE_EQUALS_NO_HASHCODE - equals without hashCode ---
    private String name;

    public SpotBugsDemo(String name) {
        this.name = name;
    }

    @Override
    public boolean equals(Object obj) {
        if (this == obj) return true;
        if (obj == null || getClass() != obj.getClass()) return false;
        SpotBugsDemo other = (SpotBugsDemo) obj;
        return name != null ? name.equals(other.name) : other.name == null;
    }
    // Missing hashCode() override!

    // --- Bug 4: ES_COMPARING_STRINGS_WITH_EQ - string == comparison ---
    public boolean isAdmin(String role) {
        // Bug: comparing strings with == instead of .equals()
        if (role == "admin") {
            return true;
        }
        return false;
    }

    // --- Bug 5: NP_NULL_ON_SOME_PATH - null dereference after null check ---
    public int getNameLength(String input) {
        if (input == null) {
            System.out.println("Input is null");
        }
        // Bug: input may be null here
        return input.length();
    }

    public static void main(String[] args) {
        SpotBugsDemo demo = new SpotBugsDemo("classroom");
        System.out.println("SpotBugs Demo 1 - Report Generation");
        System.out.println("This code contains intentional simple mistakes.");
        System.out.println("Scores count: " + demo.getScores().length);
        System.out.println("Is admin: " + demo.isAdmin("admin"));
    }
}
